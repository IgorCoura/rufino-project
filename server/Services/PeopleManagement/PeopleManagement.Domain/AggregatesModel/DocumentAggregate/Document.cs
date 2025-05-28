using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class Document: Entity, IAggregateRoot
    {
        public Name Name { get; private set; }
        public Description Description { get; private set; }
        public Guid EmployeeId { get; private set; }
        public Guid CompanyId { get; private set; }
        public Guid RequiredDocumentId { get; private set; }
        public List<DocumentUnit> DocumentsUnits { get; private set; } = [];
        public DocumentStatus Status { get; private set; } = DocumentStatus.RequiresDocument;
        public Guid DocumentTemplateId { get; private set; }

        private Document(Guid id, Guid employeeId, Guid companyId, Guid requiredDocumentId, Guid documentTemplateId, Name name, Description description) : base(id)
        {
            EmployeeId = employeeId;
            CompanyId = companyId;
            RequiredDocumentId = requiredDocumentId;
            DocumentTemplateId = documentTemplateId;
            Description = description;
            Name = name;
        }

        public static Document Create(Guid id, Guid employeeId, Guid companyId, Guid requiredDocumentId, Guid documentTemplateId, Name name, Description description) => new(id, employeeId, companyId, requiredDocumentId, documentTemplateId, name, description);

        public DocumentUnit NewDocumentUnit(Guid documentUnitId)
        {
            if (DocumentsUnits.Any(x => x.Status == DocumentUnitStatus.Pending))
                return DocumentsUnits.FirstOrDefault(x => x.Status == DocumentUnitStatus.Pending)!;

            var documentUnit = DocumentUnit.Create(documentUnitId, this);
            DocumentsUnits.Add(documentUnit);
            return documentUnit;
        }

        public void InsertUnitWithRequireValidation(Guid documentUnitId, Name name, Extension extension)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.InsertWithRequireValidation(name, extension);

            Status = DocumentStatus.RequiresValidation;
            
        }

        public string InsertUnitWithoutRequireValidation(Guid documentUnitId, Name name, Extension extension)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.InsertWithoutRequireValidation(name, extension);

            Status = DocumentStatus.OK;

            DeprecateDocumentsUnit(documentUnitId);

            return documentUnit.GetNameWithExtension;
        }

        public DocumentUnit UpdateDocumentUnitDetails(Guid documentUnitId, DateOnly date, DateOnly? validity, string content)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.UpdateDetails(date, validity, content);
            return documentUnit;
        }

        public DocumentUnit UpdateDocumentUnitDetails(Guid documentUnitId, DateOnly date, TimeSpan? validity, string content)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.UpdateDetails(date, validity, content);
            return documentUnit;
        }


        public void MarkAsAwaitingDocumentUnitSignature(Guid documentUnitId)
        {
            Status = DocumentStatus.AwaitingSignature;
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.MarkAsAwaitingSignature();
        }

        public void ValidateDocumentUnit(Guid documentUnitId, bool IsValid)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            document.Validate(IsValid);
            if (IsValid)
            {
                Status = DocumentStatus.OK;
                DeprecateDocumentsUnit(documentUnitId);
            }
        }

        public void MakeAsDocumentExpired(Guid documentUnitIdExpire, Guid newDocumentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitIdExpire)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitIdExpire.ToString()));

            documentUnit.MarkAsDeprecatedOrInvalid();

            NewDocumentUnit(newDocumentUnitId);
        }

        public void MakeAsDeprecated()
        {
            Status = DocumentStatus.Deprecated;
            DeprecateDocumentsUnit();
        }

        private void DeprecateDocumentsUnit(Guid? exceptionDocumentId = null)
        {
            DocumentsUnits.ForEach(x =>
            {
                if(exceptionDocumentId == null || x.Id != exceptionDocumentId)                 
                    x.MarkAsDeprecatedOrInvalid();
            });
        }

        public void MarkAsNotApplicableDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            document.MarkAsNotApplicable();
            Status = DocumentStatus.OK;
            DeprecateDocumentsUnit(exceptionDocumentId: documentUnitId);
        }

        public bool IsAwaitingSignatureDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            return document.IsAwaitingSignature;
        }

        public bool CanEditDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            return document.CanEdit;
        }

        public bool IsPendingDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            return document.IsPending;
        }

        public bool CanBeDeleted()
        {
            if(DocumentsUnits.Count == 0)
                return true;    
            return DocumentsUnits.Any(x => x.IsPending == false) == false;
        }

        public static DocumentStatus GetRepresentingStatus(List<DocumentStatus> documentsStatus)
        {
            var result = documentsStatus.FirstOrDefault(x => x.Id == DocumentStatus.RequiresDocument.Id);
            if (result is not null)
                return result;

            result = documentsStatus.FirstOrDefault(x => x.Id == DocumentStatus.RequiresValidation.Id);
            if (result is not null)
                return result;

            result = documentsStatus.FirstOrDefault(x => x.Id == DocumentStatus.AwaitingSignature.Id);
            if (result is not null)
                return result;

            result = documentsStatus.FirstOrDefault(x => x.Id == DocumentStatus.OK.Id);
            if (result is not null)
                return result;

            if (documentsStatus.Count == 0)
                return DocumentStatus.OK;


            return documentsStatus.First();
        }

        private void ChangeStatus(DocumentStatus status)
        {
            Status = status;
        }

        private bool HasValidDocumentsUnit()
        {
            return DocumentsUnits.Any(x => x.IsOK);
        }
    }
}
