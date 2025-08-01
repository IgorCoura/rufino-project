using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
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
        public DocumentStatus Status { get; private set; } = DocumentStatus.OK;
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

        public DocumentUnit GetDocumentUnit(Guid documentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            return documentUnit;
        }
        public void MarkAsAwaitingDocumentUnitSignature(Guid documentUnitId)
        {
            
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            var isAwaitingSignature = documentUnit.MarkAsAwaitingSignature();
            if(isAwaitingSignature)
                Status = DocumentStatus.AwaitingSignature;
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

        public bool MakeAsDocumentDeprecated(Guid documentUnitIdExpire, Guid newDocumentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitIdExpire)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitIdExpire.ToString()));

            var isDeprecatedOrInvalid = documentUnit.MarkAsDeprecatedOrInvalid();
            Status = DocumentStatus.Deprecated;
            return isDeprecatedOrInvalid;
        }

        public bool MakeAsWarning(Guid documentUnitIdExpire)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitIdExpire)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitIdExpire.ToString()));

            var isMarkAsWarning =  documentUnit.MarkAsWarning();

            if(isMarkAsWarning)
            {
                Status = DocumentStatus.Warning;
                return true;
            }
            return false;
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

        public bool MarkAsNotApplicableDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            var isNotApplicable = document.MarkAsNotApplicable();
            if(isNotApplicable)
            {
                Status = DocumentStatus.OK;
                DeprecateDocumentsUnit(exceptionDocumentId: documentUnitId);
                return true;
            }
            return false;
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
            var result = documentsStatus.OrderBy(x => DocumentStatus.GetOrder(x))
                .ToList();

            if (result.Count == 0)
                return DocumentStatus.OK;

            return documentsStatus.First();
        }

    }
}
