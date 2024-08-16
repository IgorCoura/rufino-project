using Azure.Storage.Blobs;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;

namespace PeopleManagement.Infra.Services
{
    public class BlobAzureService(BlobServiceClient blobServiceClient) : IBlobService
    {
        public async Task UploadAsync(Stream stream, string fileNameWithExtesion, string containerName, CancellationToken cancellationToken = default)
        {
            BlobClient blobClient = await GetBlobClientAsync(fileNameWithExtesion, containerName, cancellationToken);
            await blobClient.UploadAsync(stream,  cancellationToken);
        }

        public async Task<Stream> DownloadAsync(string fileNameWithExtesion, string containerName, CancellationToken cancellationToken = default)
        {
            BlobClient blobClient = await GetBlobClientAsync(fileNameWithExtesion, containerName, cancellationToken);
            using var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
            return stream;
        }

        public async Task DeleteAsync(string fileNameWithExtesion, string containerName, CancellationToken cancellationToken = default)
        {
            BlobClient blobClient = await GetBlobClientAsync(fileNameWithExtesion, containerName, cancellationToken);

            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        private async Task<BlobClient> GetBlobClientAsync(string fileNameWithExtesion, string containerName, CancellationToken cancellationToken = default)
        { 
            var containerClient = await GetBlobContainerClient(containerName, cancellationToken);

            BlobClient blobClient = containerClient.GetBlobClient(fileNameWithExtesion);

            return blobClient;
        }

        private async Task<BlobContainerClient> GetBlobContainerClient(string containerName, CancellationToken cancellationToken = default)
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            return containerClient;
        }
    }
}
