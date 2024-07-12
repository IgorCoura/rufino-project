using System.IO.Compression;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces
{
    public interface ILocalStorageService
    {
        Task UnzioUploadAsync(Stream stream, string destinationDirectoryName, string sourceDestinationDirectoryPath, CancellationToken cancellationToken = default);
        Task<Stream> ZipDownloadAsync(string originDirectoryName, string sourceOriginDirectoryPath, CancellationToken cancellationToken = default);
        Task DeleteAsync(string originDirectoryName, string sourceOriginDirectoryPath, CancellationToken cancellationToken = default);
    }
}
