using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate
{
    public class SecurityDocument: Entity, IAggregateRoot
    {
        public Guid EmployeeId { get; private set; }
        public Guid CompanyId { get; private set; }
        public Guid RoleId { get; private set; }
        public List<Document> Documents { get; private set; } = [];
        public SecurityDocumentStatus Status { get; private set; } = SecurityDocumentStatus.RequiredDocument;
        public DocumentType Type { get; private set; }

        private SecurityDocument(Guid employeeId, Guid companyId, Guid roleId, DocumentType type)
        {
            EmployeeId = employeeId;
            CompanyId = companyId;
            RoleId = roleId;
            Type = type;
        }

        public static SecurityDocument Create(Guid employeeId, Guid companyId, Guid roleId, DocumentType type) => new(employeeId, companyId, roleId, type);

        public void AddDocument(Document document)
        {
            Documents.Add(document);
        }

        public void InsertDocumentWithRequireValidation(Guid documentId, Name name, Extension extension)
        {
            var document = Documents.FirstOrDefault(x => x.Id == documentId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            DateTime? validity = null;
            if (Type.ValidityInterval != null)
                validity = document.Date.Add((TimeSpan)Type.ValidityInterval);

            document.InsertFileWithRequireValidation(name, extension, validity);

            Status = SecurityDocumentStatus.RequiredValidaty;
        }

        public void InsertDocumentWithoutRequireValidation(Guid documentId, Name name, Extension extension)
        {
            var document = Documents.FirstOrDefault(x => x.Id == documentId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            DateTime? validity = null;
            if (Type.ValidityInterval != null)
                validity = document.Date.Add((TimeSpan)Type.ValidityInterval);

            document.InsertFileWithRequireValidation(name, extension, validity);

            Status = SecurityDocumentStatus.OK;

            DeprecateOldDocuments(documentId);
        }

        public void ValidateDocument(Guid documentId, bool IsValid)
        {
            var document = Documents.FirstOrDefault(x => x.Id == documentId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            document.Validate(IsValid);
            if (IsValid)
            {
                Status = SecurityDocumentStatus.OK;
                DeprecateOldDocuments(documentId);
            }

            if (HasValidDocument())
            {
                Status = SecurityDocumentStatus.OK;
            }
            else
            {
                Status = SecurityDocumentStatus.RequiredDocument;
            }
        }

        public void HasOverdueDocuments()
        {
            var hasOverDueDocuments = Documents.Any(x => x.Status == DocumentStatus.OK && x.Validity > DateTime.UtcNow);
            DeprecateOldDocuments();
            if(hasOverDueDocuments)
                Status = SecurityDocumentStatus.RequiredDocument;
        }

        private void DeprecateOldDocuments(Guid? exceptionDocumentId = null)
        {
            Documents.ForEach(x =>
            {
                if(exceptionDocumentId == null || x.Id != exceptionDocumentId)                 
                    x.Deprecate();
            });
        }

        private bool HasValidDocument()
        {
            return Documents.Any(x => x.IsOK);
        }
    }
}
