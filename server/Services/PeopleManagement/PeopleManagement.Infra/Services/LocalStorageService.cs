using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.IO.Compression;

namespace PeopleManagement.Infra.Services
{
    public class LocalStorageService(ILogger<ILocalStorageService> logger) : ILocalStorageService
    {
        private readonly ILogger<ILocalStorageService> _logger = logger;
        public  Task UnzipUploadAsync(Stream stream, string destinationDirectoryName, string sourceDestinationDirectoryPath, CancellationToken cancellationToken = default)
        {

            var destinationDirectoryPath = VerifyAndCreateDirectoryIfNotExist(destinationDirectoryName, sourceDestinationDirectoryPath);
            _logger.LogInformation("Unzipping file to directory: {DestinationDirectoryPath} in the currentDirectory: {currentDirectory}", destinationDirectoryPath, Directory.GetCurrentDirectory());
            try
            {
                using var zipArchive = new ZipArchive(stream);
                zipArchive.ExtractToDirectory(destinationDirectoryPath, overwriteFiles: true);  

                return Task.CompletedTask;
            }
            catch(InvalidDataException)
            {
                throw new DomainException(this, InfraErrors.File.InvalidFile());
            }
        }

        public Task<Stream> ZipDownloadAsync(string originDirectoryName, string sourceOriginDirectoryPath, CancellationToken cancellationToken = default)
        {

            _logger.LogInformation("Zipping directory: {OriginDirectoryName} at path: {Path}", originDirectoryName, Directory.GetCurrentDirectory());
            var originDirectoryPath = VerifyAndCreateDirectoryIfNotExist(originDirectoryName, sourceOriginDirectoryPath);
            var stream = new MemoryStream();
            ZipFile.CreateFromDirectory(originDirectoryPath, stream);
            stream.Position = 0;
            return Task.FromResult((Stream)stream);
        }

        public Task<bool> HasFile(string originDirectoryName, string sourceOriginDirectoryPath, CancellationToken cancellationToken = default)
        {
            var originDirectoryPath = VerifyAndCreateDirectoryIfNotExist(originDirectoryName, sourceOriginDirectoryPath);
            if (Directory.EnumerateDirectories(originDirectoryPath).Any() || Directory.EnumerateFiles(originDirectoryPath).Any())
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task DeleteAsync(string originDirectoryName, string sourceOriginDirectoryPath, CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(originDirectoryName, sourceOriginDirectoryPath);
            var originDirectoryPath = VerifyAndCreateDirectoryIfNotExist(originDirectoryName, path);
            Directory.Delete(originDirectoryPath, true);
            return Task.CompletedTask;
        }

        private string VerifyAndCreateDirectoryIfNotExist(string directoryName, string sourceDirectoryPath)
        {
            var directoryPath = Path.Combine(sourceDirectoryPath, directoryName);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }
    }
}
