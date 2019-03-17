using Amazon.Runtime;
using SyncHole.Core.Exceptions;

namespace SyncHole.Core.Client.AWS
{
    public static class AWSExtensions
    {
        public static bool IsSuccess(this AmazonWebServiceResponse response)
        {
            return (int)response.HttpStatusCode >= 200
                   && (int)response.HttpStatusCode <= 207;
        }

        public static void EnsureSuccess(this AmazonWebServiceResponse response)
        {
            if (!response.IsSuccess())
            {
                throw new OperationFailedException<AmazonWebServiceResponse>(
                    "Http request not OK", response);
            }
        }
    }
}
