using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;

namespace PeopleManagement.Infra.Services
{
    public class BlobS3Service(IAmazonS3 s3Client) : IBlobService
    {
        public async Task UploadAsync(Stream stream, string fileNameWithExtesion, string containerName, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            await EnsureBucketExistsAsync(containerName, cancellationToken);

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = containerName,
                Key = fileNameWithExtesion,
                InputStream = memoryStream,
                AutoCloseStream = false,
                UseChunkEncoding = false
            };

            await s3Client.PutObjectAsync(request, cancellationToken);
        }

        public async Task<Stream> DownloadAsync(string fileNameWithExtesion, string containerName, CancellationToken cancellationToken = default)
        {
            var request = new GetObjectRequest
            {
                BucketName = containerName,
                Key = fileNameWithExtesion
            };

            var response = await s3Client.GetObjectAsync(request, cancellationToken);
            return response.ResponseStream;
        }

        public async Task DeleteAsync(string fileNameWithExtesion, string containerName, CancellationToken cancellationToken = default)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = containerName,
                Key = fileNameWithExtesion
            };

            await s3Client.DeleteObjectAsync(request, cancellationToken);
        }

        private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
        {
            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName);
            if (!exists)
                await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName }, cancellationToken);
        }
    }
}
