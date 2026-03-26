namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models
{
    public record SessionSignedDocumentsModel(
        string SignerToken,
        string PrimarySignedFileUrl,
        IReadOnlyList<AttachmentSignedFile> Attachments);

    public record AttachmentSignedFile(string AttachmentToken, string SignedFileUrl);
}
