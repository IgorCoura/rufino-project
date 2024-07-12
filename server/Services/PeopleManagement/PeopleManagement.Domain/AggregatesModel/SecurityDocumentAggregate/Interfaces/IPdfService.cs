using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> ConvertHtml2Pdf(DocumentTemplate type, string content, CancellationToken cancellationToken = default);
    }
}
