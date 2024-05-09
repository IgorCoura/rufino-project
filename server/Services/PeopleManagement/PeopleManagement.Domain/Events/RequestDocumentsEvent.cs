using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;

namespace PeopleManagement.Domain.Events
{
    public record RequestDocumentsEvent : INotification
    {
        public Guid OwnerId { get; private set; }
        public Guid CompanyId { get; private set; }
        public Category[] Categories { get; private set; }

        private RequestDocumentsEvent(Guid ownerId, Guid companyId, Category[] categories)
        {
            OwnerId = ownerId;
            CompanyId = companyId;
            Categories = categories;
        }

        public static RequestDocumentsEvent IdCard(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.IdCard]);
        public static RequestDocumentsEvent VoteId(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.VoteId]);
        public static RequestDocumentsEvent AddressProof(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.AddressProof]);
        public static RequestDocumentsEvent Contract(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.Contract]);        
        public static RequestDocumentsEvent MarriageCertificate(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.MarriageCertificate]);
        public static RequestDocumentsEvent ChildDocument(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.ChildDocument]);
        public static RequestDocumentsEvent EducationalCertificate(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.EducationalCertificate]);
        public static RequestDocumentsEvent VaccinationCertificate(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.VaccinationCertificate]);
        public static RequestDocumentsEvent MilitarDocument(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.MilitaryDocument]);
        public static RequestDocumentsEvent MedicalAdmissionExam(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.MedicalAdmissionExam]);
        public static RequestDocumentsEvent SpouseDocument(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.SpouseDocument]);
        public static RequestDocumentsEvent MedicalDismissalExam(Guid ownerId, Guid companyId) => new(ownerId, companyId, [Category.MedicalDismissalExam]);

    }
}


