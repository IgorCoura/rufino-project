using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.Options;
using System.IO.Compression;

namespace PeopleManagement.Infra.Services
{
    public class LocalStorageService(LocalStorageOptions options, ILogger<ILocalStorageService> logger) : ILocalStorageService
    {
        private readonly LocalStorageOptions _options = options;
        private readonly ILogger<ILocalStorageService> _logger = logger;
        public  Task UnzipUploadAsync(Stream stream, string destinationDirectoryName, string sourceDestinationDirectoryPath, CancellationToken cancellationToken = default)
        {

            var path = Path.Combine(_options.RootPath, sourceDestinationDirectoryPath);
            var destinationDirectoryPath = VerifyAndCreateDirectoryIfNotExist(destinationDirectoryName, path);
            _logger.LogInformation("Unzipping file to directory: {DestinationDirectoryPath}", destinationDirectoryPath);
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
            var path = Path.Combine(_options.RootPath, sourceOriginDirectoryPath);
            _logger.LogInformation("Zipping directory: {OriginDirectoryName} at path: {Path}", originDirectoryName, path);
            var originDirectoryPath = VerifyAndCreateDirectoryIfNotExist(originDirectoryName, path);
            var stream = new MemoryStream();
            ZipFile.CreateFromDirectory(originDirectoryPath, stream);
            stream.Position = 0;
            return Task.FromResult((Stream)stream);
        }

        public Task<bool> HasFile(string originDirectoryName, string sourceOriginDirectoryPath, CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(_options.RootPath, sourceOriginDirectoryPath);
            var originDirectoryPath = VerifyAndCreateDirectoryIfNotExist(originDirectoryName, path);
            if (Directory.EnumerateDirectories(originDirectoryPath).Any() || Directory.EnumerateFiles(originDirectoryPath).Any())
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task DeleteAsync(string originDirectoryName, string sourceOriginDirectoryPath, CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(_options.RootPath, sourceOriginDirectoryPath);
            var originDirectoryPath = VerifyAndCreateDirectoryIfNotExist(originDirectoryName, path);
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
