using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models;
using Extension = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Extension;

namespace PeopleManagement.Infra.Services
{
    public class FileDownloadService : IFileDownloadService
    {
        private readonly HttpClient _httpClient;

        public FileDownloadService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<FileDownloadModel> DownloadFileFromUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var extension = GetFileExtension(response.Content.Headers.ContentType?.MediaType);

            return new FileDownloadModel(stream, extension);
        }

        private static Extension GetFileExtension(string? contentType)
        {
            var extensionString = contentType?.ToLower() switch
            {
                "application/pdf" => "PDF",
                "image/jpeg" => "JPEG",
                "image/jpg" => "JPG",
                "image/png" => "PNG",
                _ => "PDF"
            };

            return (Extension)extensionString;
        }
    }
}
