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
            RefreshDocumentStatus();
            return documentUnit;
        }

        public void InsertUnitWithRequireValidation(Guid documentUnitId, Name name, Extension extension)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.InsertWithRequireValidation(name, extension);

            RefreshDocumentStatus();

        }

        public string InsertUnitWithoutRequireValidation(Guid documentUnitId, Name name, Extension extension)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.InsertWithoutRequireValidation(name, extension);


            DeprecateDocumentsUnit(documentUnitId);
            RefreshDocumentStatus();
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

            documentUnit.MarkAsAwaitingSignature();
            RefreshDocumentStatus();
        }

        public void MarkAsInvalidDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            document.MaskAsInvalid();

            RefreshDocumentStatus();
        }

        public void MarkAsValidDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            document.MaskAsValid();
  
            DeprecateDocumentsUnit(documentUnitId);
            RefreshDocumentStatus();
        }

        public bool MakeAsDocumentDeprecated(Guid documentUnitIdExpire, Guid newDocumentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitIdExpire)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitIdExpire.ToString()));

            var isDeprecatedOrInvalid = documentUnit.MarkAsDeprecatedOrInvalid();
            RefreshDocumentStatus();
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
            DeprecateDocumentsUnit();
            RefreshDocumentStatus();
        }

        private void DeprecateDocumentsUnit(Guid? exceptionDocumentId = null)
        {
            DocumentsUnits.ForEach(x =>
            {
                if(exceptionDocumentId == null || x.Id != exceptionDocumentId)                 
                    x.MarkAsDeprecatedOrInvalid();
            });
            RefreshDocumentStatus();
        }

        public bool MarkAsNotApplicableDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            var isNotApplicable = document.MarkAsNotApplicable();
            if(isNotApplicable)
            {
                DeprecateDocumentsUnit(exceptionDocumentId: documentUnitId);
                RefreshDocumentStatus();
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

        private void RefreshDocumentStatus()
        {
            if (DocumentsUnits.Count == 0)
            {
                Status = DocumentStatus.OK;
                return;
            }

            if (DocumentsUnits.Any(x => x.Status == DocumentUnitStatus.OK))
            {
                Status = DocumentStatus.OK;
                return;
            }

            if (DocumentsUnits.Any(x => x.Status == DocumentUnitStatus.RequiresValidation))
            {
                Status = DocumentStatus.RequiresValidation;
                return;
            }

            if (DocumentsUnits.Any(x => x.Status == DocumentUnitStatus.AwaitingSignature))
            {
                Status = DocumentStatus.AwaitingSignature;
                return;
            }

            if (DocumentsUnits.Any(x => x.Status == DocumentUnitStatus.Warning))
            {
                Status = DocumentStatus.Warning;
                return;
            }

            if (DocumentsUnits.Any(x => x.Status == DocumentUnitStatus.NotApplicable))
            {
                Status = DocumentStatus.OK;
                return;
            }

            if (DocumentsUnits.Any(x => x.Status == DocumentUnitStatus.Deprecated))
            {
                Status = DocumentStatus.Deprecated;
                return;
            }

            Status = DocumentStatus.RequiresDocument;
        }



    }
}
