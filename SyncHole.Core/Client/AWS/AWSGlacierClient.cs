using Amazon;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using SyncHole.Core.Exceptions;
using SyncHole.Core.Model;
using System;
using System.Threading.Tasks;

namespace SyncHole.Core.Client.AWS
{
    public class AWSGlacierClient : IStorageClient
    {
        private readonly AmazonGlacierClient _client;
        private const long DefaultChunkSize = 4194304; //4 MB chuck size

        public event StartedEventHandler Started;
        public event ProgressEventHandler Progress;
        public event FailureEventHandler Failed;
        public event CompletedEventHandler Completed;

        public AWSGlacierClient(AWSCredentials credentials)
        {
            _client = new AmazonGlacierClient(
                credentials.AccessKeyId, credentials.AccessSecretKey,
                RegionEndpoint.GetBySystemName(credentials.ServiceEndpoint));
        }

        public async Task<UploadJob> InitializeAsync(
            string containerName, string description, long? chunkSize = null)
        {
            //create the vault if not exists
            await CreateVaultAsync(containerName);

            //prepare request
            var request = new InitiateMultipartUploadRequest
            {
                VaultName = containerName,
                PartSize = chunkSize ?? DefaultChunkSize,
                ArchiveDescription = description
            };

            //create place holder for parts of a archive
            var response = await ExecuteAsync(_client.InitiateMultipartUploadAsync(request));
            var job = new UploadJob
            {
                UploadId = response.UploadId,
                VaultName = containerName,
                ChunkSize = chunkSize ?? DefaultChunkSize
            };

            //trigger the started event
            OnStarted(job);

            return job;
        }

        private async Task CreateVaultAsync(string containerName)
        {
            var createRequest = new CreateVaultRequest { VaultName = containerName };
            var createResponse = await ExecuteAsync(_client.CreateVaultAsync(createRequest));
            if (!createResponse.IsSuccess())
            {
                throw new OperationFailedException<CreateVaultResponse>("Unable to create the container", createResponse);
            }
        }

        public async Task UploadChunkAsync(UploadJob job, UploadItem item)
        {
            //create reference to a part of the input stream
            var chunkStream = GlacierUtils.CreatePartStream(item.DataStream, job.ChunkSize);
            var chunkChecksum = TreeHashGenerator.CalculateTreeHash(chunkStream);

            //prepare request
            var request = new UploadMultipartPartRequest
            {
                VaultName = job.VaultName,
                Body = item.DataStream,
                Checksum = chunkChecksum,
                UploadId = job.UploadId
            };

            //set range of the current part
            request.SetRange(job.CurrentPosition,
                job.CurrentPosition + chunkStream.Length - 1);

            //upload this part
            var response = await ExecuteAsync(_client.UploadMultipartPartAsync(request), job);
            response.EnsureSuccess();

            //commit progress
            job.ChunkChecksums.Add(chunkChecksum);
            job.CurrentPosition += chunkStream.Length;

            //trigger the progress event
            OnProgress(job);
        }

        public async Task<string> FinishUploadAsync(UploadJob job, UploadItem item)
        {
            var checksum = TreeHashGenerator.CalculateTreeHash(job.ChunkChecksums);

            //prepare request
            var request = new CompleteMultipartUploadRequest
            {
                UploadId = job.UploadId,
                ArchiveSize = item.ContentLength.ToString(),
                Checksum = checksum,
                VaultName = job.VaultName
            };

            //finish up multipart upload
            var response = await ExecuteAsync(_client.CompleteMultipartUploadAsync(request), job);
            var achiveId = response.ArchiveId;

            //trigger the completion event
            OnCompleted(job, achiveId);

            return achiveId;
        }

        #region Event Handlers
        protected virtual void OnStarted(UploadJob job)
        {
            Started?.Invoke(job);
        }

        protected virtual void OnProgress(UploadJob job)
        {
            Progress?.Invoke(job);
        }

        protected virtual void OnFailed(UploadJob job, Exception ex)
        {
            Failed?.Invoke(job);
        }

        protected virtual void OnCompleted(UploadJob job, string archiveId)
        {
            Completed?.Invoke(job, archiveId);
        }
        #endregion Event Handlers

        private async Task<T> ExecuteAsync<T>(Task<T> task, UploadJob job = null)
        {
            try
            {
                return await task;
            }
            catch (Exception ex)
            {
                OnFailed(job, ex);
                throw new OperationFailedException<UploadJob>("Failed to perform specified task", job, ex);
            }
        }
    }
}