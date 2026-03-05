using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Reflection.Metadata.Ecma335;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class Document: Entity, IAggregateRoot
    {
        private DocumentStatus _documentStatus = DocumentStatus.OK;
        public Name Name { get; private set; }
        public Description Description { get; private set; }
        public Guid EmployeeId { get; private set; }
        public Guid CompanyId { get; private set; }
        public Guid RequiredDocumentId { get; private set; }
        public List<DocumentUnit> DocumentsUnits { get; private set; } = [];
        public DocumentStatus Status {
            get => _documentStatus;
            private set 
            { 
                if(_documentStatus != value)
                {
                    var oldValue = _documentStatus;
                    _documentStatus = value;
                    AddDomainEvent(new DocumentStatusChangedDomainEvent(this.Id, this.EmployeeId, this.CompanyId, oldValue, value));
                }
            } 
        } 
        public Guid DocumentTemplateId { get; private set; }
        public bool UsePreviousPeriod { get; private set; }

        private Document(Guid id, Guid employeeId, Guid companyId, Guid requiredDocumentId, Guid documentTemplateId, Name name, Description description,
            bool usePreviousPeriod = false) : base(id)
        {
            EmployeeId = employeeId;
            CompanyId = companyId;
            RequiredDocumentId = requiredDocumentId;
            DocumentTemplateId = documentTemplateId;
            Description = description;
            Name = name;
            UsePreviousPeriod = usePreviousPeriod;
        }

        public static Document Create(Guid id, Guid employeeId, Guid companyId, Guid requiredDocumentId, Guid documentTemplateId, Name name, Description description,
            bool usePreviousPeriod = false) => new(id, employeeId, companyId, requiredDocumentId, documentTemplateId, name, description, usePreviousPeriod);

        public DocumentUnit NewDocumentUnit(Guid documentUnitId, PeriodType? periodType = null, DateTime? referenceDate = null)
        {
            if (DocumentsUnits.Any(x => x.Status == DocumentUnitStatus.Pending))
                return DocumentsUnits.FirstOrDefault(x => x.Status == DocumentUnitStatus.Pending)!;

            var documentUnit = DocumentUnit.Create(documentUnitId, this, periodType, referenceDate);
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

        public DocumentUnit UpdateDocumentUnitDetails(Guid documentUnitId, DateOnly date, TimeSpan? validity, string content, PeriodType periodType)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.UpdateDetails(date, validity, content, periodType);
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
            if (this.UsePreviousPeriod == true)
            {
                // Agrupa DocumentUnits por Period
                var groupedByPeriod = DocumentsUnits
                    .Where(x => x.Period != null)
                    .GroupBy(x => x.Period);

                foreach (var periodGroup in groupedByPeriod)
                {
                    // Encontra o DocumentUnit OK mais recente no período (baseado na data de criação/Id)
                    var okDocuments = periodGroup.Where(x => x.IsOK).OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList();

                    if (okDocuments.Count > 1)
                    {
                        // Mantém apenas o mais recente, deprecia os outros
                        var mostRecent = okDocuments.First();
                        foreach (var doc in okDocuments.Skip(1))
                        {
                            if (exceptionDocumentId == null || doc.Id != exceptionDocumentId)
                            {
                                doc.MarkAsDeprecatedOrInvalid();
                            }
                        }
                    }
                }

                // Deprecia os documentos sem período ou que não são OK
                DocumentsUnits.ForEach(x =>
                {
                    if (x.Period == null && !x.IsOK)
                    {
                        if (exceptionDocumentId == null || x.Id != exceptionDocumentId)
                            x.MarkAsDeprecatedOrInvalid();
                    }
                });
            }
            else
            {
                DocumentsUnits.ForEach(x =>
                {
                    if (exceptionDocumentId == null || x.Id != exceptionDocumentId)
                        x.MarkAsDeprecatedOrInvalid();
                });
            }

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

            var unitsWithPeriod = DocumentsUnits.Where(x => x.Period != null).ToList();
            var unitsWithoutPeriod = DocumentsUnits.Where(x => x.Period == null).ToList();

            if (unitsWithPeriod.Count > 0)
            {
                var periodStatuses = unitsWithPeriod
                    .GroupBy(x => x.Period)
                    .Select(g => GetStatusFromGroup(g));
                if (periodStatuses.Any(x => x == DocumentStatus.RequiresDocument))
                    Status = DocumentStatus.RequiresDocument;
                if (periodStatuses.Any(x => x == DocumentStatus.RequiresValidation))
                    Status = DocumentStatus.RequiresValidation;
                if (periodStatuses.Any(x => x == DocumentStatus.Warning))
                    Status = DocumentStatus.Warning;    
                Status = DocumentStatus.OK;
            } 
            else
            {
                var status = GetStatusFromGroup(DocumentsUnits);
                Status = status;
            }
        }

        private static DocumentStatus GetStatusFromGroup(IEnumerable<DocumentUnit> units)
        {
            if (!units.Any())
                return DocumentStatus.OK;

            if (units.Any(x => x.Status == DocumentUnitStatus.OK))
                return DocumentStatus.OK;

            if (units.Any(x => x.Status == DocumentUnitStatus.RequiresValidation))
                return DocumentStatus.RequiresValidation;

            if (units.Any(x => x.Status == DocumentUnitStatus.AwaitingSignature))
                return DocumentStatus.AwaitingSignature;

            if (units.Any(x => x.Status == DocumentUnitStatus.Warning))
                return DocumentStatus.Warning;

            if (units.Any(x => x.Status == DocumentUnitStatus.NotApplicable))
                return DocumentStatus.OK;

            if (units.Any(x => x.Status == DocumentUnitStatus.Deprecated))
                return DocumentStatus.Deprecated;

            return DocumentStatus.RequiresDocument;
        }



    }
}
