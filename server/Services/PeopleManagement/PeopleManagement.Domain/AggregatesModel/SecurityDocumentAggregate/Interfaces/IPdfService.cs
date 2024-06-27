namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> ConvertHtml2Pdf(DocumentType type, string content, CancellationToken cancellationToken = default);
    }
}
