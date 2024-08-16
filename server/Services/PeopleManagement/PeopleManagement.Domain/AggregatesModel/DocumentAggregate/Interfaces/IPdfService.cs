using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> ConvertHtml2Pdf(DocumentTemplate type, string content, CancellationToken cancellationToken = default);
    }
}
