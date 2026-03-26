namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models
{
    public record WebhookDocumentEventModel(
        Guid DocumentUnitId,
        WebhookDocumentStatus Status,
        string Url = "",
        string fileExtesion = "PDF",
        IReadOnlyList<WebhookExtraDocModel>? ExtraDocs = null
    );

    public record WebhookExtraDocModel(string Token, string SignedFileUrl);

    public enum WebhookDocumentStatus
    {
        DocCreated,
        DocSigned,
        DocRefused,
        DocDeleted,
        DocExpired,
        Unknown
    }
}
