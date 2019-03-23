using Amazon;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using SyncHole.Core.Exceptions;
using SyncHole.Core.Model;
using System.Threading.Tasks;

namespace SyncHole.Core.Client.AWS
{
    public class AWSGlacierClient : IStorageClient
    {
        private readonly AmazonGlacierClient _client;
        private const long DefaultChunkSize = 4194304; //4 MB chuck size

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
            var response = await _client.InitiateMultipartUploadAsync(request);
            var job = new UploadJob
            {
                UploadId = response.UploadId,
                VaultName = containerName,
                ChunkSize = chunkSize ?? DefaultChunkSize
            };

            return job;
        }

        private async Task CreateVaultAsync(string containerName)
        {
            var createRequest = new CreateVaultRequest { VaultName = containerName };
            var createResponse = await _client.CreateVaultAsync(createRequest);
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
                Body = chunkStream,
                Checksum = chunkChecksum,
                UploadId = job.UploadId
            };

            //set range of the current part
            request.SetRange(job.CurrentPosition,
                job.CurrentPosition + chunkStream.Length - 1);

            //upload this part
            var response = await _client.UploadMultipartPartAsync(request);
            response.EnsureSuccess();

            //commit progress
            job.ChunkChecksums.Add(chunkChecksum);
            job.CurrentPosition += chunkStream.Length;
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
            var response = await _client.CompleteMultipartUploadAsync(request);
            var achiveId = response.ArchiveId;
            return achiveId;
        }
    }
}