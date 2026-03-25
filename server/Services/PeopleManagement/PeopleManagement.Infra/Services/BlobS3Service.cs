using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.Options;

namespace PeopleManagement.Infra.Services
{
    public class BlobS3Service(IAmazonS3 s3Client, IOptions<S3Options> options) : IBlobService
    {
        private readonly S3Options _options = options.Value;
        public async Task UploadAsync(Stream stream, string fileNameWithExtesion, string containerName, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            await EnsureBucketExistsAsync(containerName, cancellationToken);

            if (!overwrite && await ObjectExistsAsync(fileNameWithExtesion, containerName, cancellationToken))
                return;

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = containerName,
                Key = fileNameWithExtesion,
                InputStream = memoryStream,
                AutoCloseStream = _options.AutoCloseStream,
                UseChunkEncoding = _options.UseChunkEncoding
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
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            response.ResponseStream.Dispose();
            memoryStream.Position = 0;
            return memoryStream;
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

        private async Task<bool> ObjectExistsAsync(string key, string bucketName, CancellationToken cancellationToken = default)
        {
            try
            {
                await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = key
                }, cancellationToken);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
