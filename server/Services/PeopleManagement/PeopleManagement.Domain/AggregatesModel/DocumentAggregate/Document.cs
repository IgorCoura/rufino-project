using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Reflection.Metadata;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class Document: Entity, IAggregateRoot
    {
        public Name Name { get; private set; }
        public Description Description { get; private set; }
        public Guid EmployeeId { get; private set; }
        public Guid CompanyId { get; private set; }
        public Guid RoleId { get; private set; }
        public List<DocumentUnit> DocumentsUnits { get; private set; } = [];
        public DocumentStatus Status { get; private set; } = DocumentStatus.RequiredDocument;
        public Guid DocumentTemplateId { get; private set; }

        private Document(Guid id, Guid employeeId, Guid companyId, Guid roleId, Guid documentTemplateId, Name name, Description description) : base(id)
        {
            EmployeeId = employeeId;
            CompanyId = companyId;
            RoleId = roleId;
            DocumentTemplateId = documentTemplateId;
            Description = description;
            Name = name;
        }

        public static Document Create(Guid id, Guid employeeId, Guid companyId, Guid roleId, Guid documentTemplateId, Name name, Description description) => new(id, employeeId, companyId, roleId, documentTemplateId, name, description);

        public void AddDocument(DocumentUnit document)
        {
            DocumentsUnits.Add(document);
        }

        public void InsertUnitWithRequireValidation(Guid documentUnitId, Name name, Extension extension, TimeSpan? documentValidityDuration)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.InsertWithRequireValidation(name, extension, documentValidityDuration);

            Status = DocumentStatus.RequiredValidaty;
        }

        public string InsertUnitWithoutRequireValidation(Guid documentUnitId, Name name, Extension extension, TimeSpan? documentValidityDuration)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));
            
            documentUnit.InsertWithRequireValidation(name, extension, documentValidityDuration);

            Status = DocumentStatus.OK;

            DeprecateOldDocuments(documentUnitId);

            return documentUnit.GetNameWithExtension;
        }

        public void AwaitingDocumentUnitSignature(Guid documentUnitId)
        {
            Status = DocumentStatus.AwaitingSignature;
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.AwaitingSignature();
        }

        public void ValidateDocument(Guid documentId, bool IsValid)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentId.ToString()));

            document.Validate(IsValid);
            if (IsValid)
            {
                Status = DocumentStatus.OK;
                DeprecateOldDocuments(documentId);
            }

            if (HasValidDocument())
            {
                Status = DocumentStatus.OK;
            }
            else
            {
                Status = DocumentStatus.RequiredDocument;
            }
        }

        public void HasOverdueDocuments()
        {
            var hasOverDueDocuments = DocumentsUnits.Any(x => x.Status == DocumentUnitStatus.OK && x.Validity > DateTime.UtcNow);
            DeprecateOldDocuments();
            if(hasOverDueDocuments)
                Status = DocumentStatus.RequiredDocument;
        }

        private void DeprecateOldDocuments(Guid? exceptionDocumentId = null)
        {
            DocumentsUnits.ForEach(x =>
            {
                if(exceptionDocumentId == null || x.Id != exceptionDocumentId)                 
                    x.Deprecate();
            });
        }

        public void DocumentNotApplicable(Guid documentId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentId.ToString()));

            document.NotApplicable();
            Status = DocumentStatus.OK;
        }

        private bool HasValidDocument()
        {
            return DocumentsUnits.Any(x => x.IsOK);
        }
    }
}
