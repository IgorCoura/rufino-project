namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IBlobService
    {
        Task UploadAsync(Stream stream, string fileNameWithExtesion, string containerName, bool overwrite = false, CancellationToken cancellationToken = default);
        Task<Stream> DownloadAsync(string fileNameWithExtesion, string containerName, CancellationToken cancellationToken = default);
        Task DeleteAsync(string fileNameWithExtesion, string containerName, CancellationToken cancellationToken = default);
    }
}
