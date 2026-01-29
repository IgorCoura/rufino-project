

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models
{
    public record FileDownloadModel(Stream FileStream, Extension FileExtension);
}
