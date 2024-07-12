using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using System.IO.Compression;

namespace PeopleManagement.Infra.Services
{
    public class LocalStorageService : ILocalStorageService
    {
        public Task UnzioUploadAsync(Stream stream, string destinationDirectoryName, string sourceDestinationDirectoryPath, CancellationToken cancellationToken = default)
        {
            var destinationDirectoryPath = VerifyAndCreateDirectoryIfNotExist(destinationDirectoryName, sourceDestinationDirectoryPath);
            try
            {
                using var zipArchive = new ZipArchive(stream);
                zipArchive.ExtractToDirectory(destinationDirectoryPath);
                return Task.CompletedTask;
            }
            catch(InvalidDataException)
            {
                throw new DomainException(this, InfraErrors.File.InvalidFile());
            }
        }

        public Task<Stream> ZipDownloadAsync(string originDirectoryName, string sourceOriginDirectoryPath, CancellationToken cancellationToken = default)
        {           
            var originDirectoryPath = VerifyAndCreateDirectoryIfNotExist(originDirectoryName, sourceOriginDirectoryPath);
            using var stream = new MemoryStream();
            ZipFile.CreateFromDirectory(originDirectoryPath, stream);
            return Task.FromResult((Stream)stream);
        }

        public Task DeleteAsync(string originDirectoryName, string sourceOriginDirectoryPath, CancellationToken cancellationToken = default)
        {
            var originDirectoryPath = VerifyAndCreateDirectoryIfNotExist(originDirectoryName, sourceOriginDirectoryPath);
            Directory.Delete(originDirectoryPath, true);
            return Task.CompletedTask;
        }

        private static string VerifyAndCreateDirectoryIfNotExist(string directoryName, string sourceDirectoryPath)
        {
            var directoryPath = Path.Combine(sourceDirectoryPath, directoryName);
            if(!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }
    }
}
