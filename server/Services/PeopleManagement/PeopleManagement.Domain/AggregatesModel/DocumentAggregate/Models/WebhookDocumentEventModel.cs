namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models
{
    public record WebhookDocumentEventModel(
        Guid DocumentUnitId,
        WebhookDocumentStatus Status,
        string Url = "",
        string fileExtesion = "PDF"
    );

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
