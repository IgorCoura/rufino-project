using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IFileDownloadService
    {
        Task<FileDownloadModel> DownloadFileFromUrlAsync(string url, CancellationToken cancellationToken = default);
    }

    
}
