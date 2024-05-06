using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;

namespace PeopleManagement.Domain.Events
{
    public record RequiresDocumentsEvent : INotification
    {
        public static readonly Category[] DOCUMENTS_TO_ADMISSION = [Category.IdCard, Category.VoteId, Category.AddressProof];

        public Guid OwnerId { get; private set; }
        public Guid CompanyId { get; private set; }
        public Category[] Categories { get; private set; }

        public RequiresDocumentsEvent(Guid ownerId, Guid companyId, Category[] categories)
        {
            OwnerId = ownerId;
            CompanyId = companyId;
            Categories = categories;
        }

        public static RequiresDocumentsEvent ToAdmission(Guid ownerId, Guid companyId, bool militaryDocumentRequired) => new(ownerId, companyId, militaryDocumentRequired ? [.. DOCUMENTS_TO_ADMISSION, Category.MilitaryDocument] : DOCUMENTS_TO_ADMISSION);
    }
}
 