﻿using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using System.IO.Compression;

namespace PeopleManagement.Infra.Services
{
    public class LocalStorageService : ILocalStorageService
    {
        public  Task UnzipUploadAsync(Stream stream, string destinationDirectoryName, string sourceDestinationDirectoryPath, CancellationToken cancellationToken = default)
        {
            var destinationDirectoryPath = VerifyAndCreateDirectoryIfNotExist(destinationDirectoryName, sourceDestinationDirectoryPath);
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
