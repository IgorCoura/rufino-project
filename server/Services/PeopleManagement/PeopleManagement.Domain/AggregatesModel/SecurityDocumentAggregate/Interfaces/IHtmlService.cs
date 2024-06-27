namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces
{
    public interface IHtmlService
    {
        Task<HtmlContent> CreateHtml(DocumentType documentType, Dictionary<string, dynamic> values);
    }
}
