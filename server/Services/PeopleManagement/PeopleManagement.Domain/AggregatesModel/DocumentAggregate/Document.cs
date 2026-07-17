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

        private Document(Guid id, Guid employeeId, Guid companyId, Guid requiredDocumentId, Guid documentTemplateId, Name name, Description description) : base(id)
        {
            EmployeeId = employeeId;
            CompanyId = companyId;
            RequiredDocumentId = requiredDocumentId;
            DocumentTemplateId = documentTemplateId;
            Description = description;
            Name = name;
        }

        public static Document Create(Guid id, Guid employeeId, Guid companyId, Guid requiredDocumentId, Guid documentTemplateId, Name name, Description description)
            => new(id, employeeId, companyId, requiredDocumentId, documentTemplateId, name, description);

        /// <summary>
        /// Cria a próxima unidade do documento, reaproveitando a pendente da mesma competência quando já existir.
        ///
        /// A configuração de competência ([periodType]/[usePreviousPeriod]) vem do template do documento, lida
        /// pelo caller no momento da operação — o documento não guarda cópia: template é a configuração, a unit
        /// é a história. [referenceDate] é só a data usada para calcular em qual competência a unidade cai.
        /// </summary>
        public DocumentUnit NewDocumentUnit(Guid documentUnitId, PeriodType? periodType = null, bool usePreviousPeriod = false, DateTime? referenceDate = null)
        {
            if (periodType is not null)
            {
                var candidatePeriod = CandidatePeriodFor(periodType, usePreviousPeriod, referenceDate);

                var existingPending = DocumentsUnits.FirstOrDefault(x => x.Status == DocumentUnitStatus.Pending && x.Period != null && x.Period.Equals(candidatePeriod));

                // Candidata na competência mínima: qualquer pendente na mínima é uma unidade "esperando data",
                // reaproveitável mesmo que a granularidade do template tenha mudado desde que ela nasceu — sem
                // isso, trocar a granularidade deixaria a pendente antiga órfã e criaria uma segunda. Ela é
                // re-situada na mínima do tipo novo para não carregar a granularidade velha adiante.
                if (existingPending is null && candidatePeriod.Year == Period.MIN_YEAR)
                {
                    existingPending = DocumentsUnits.FirstOrDefault(x => x.Status == DocumentUnitStatus.Pending && x.Period != null && x.Period.Year == Period.MIN_YEAR);
                    existingPending?.ResetPeriodToMinimum(periodType);
                }

                if (existingPending is not null)
                    return existingPending;
            }
            else
            {
                var existingPending = DocumentsUnits.FirstOrDefault(x => x.Status == DocumentUnitStatus.Pending && x.Period == null);
                if (existingPending is not null)
                    return existingPending;
            }

            var documentUnit = DocumentUnit.Create(documentUnitId, this, periodType, usePreviousPeriod, referenceDate);
            DocumentsUnits.Add(documentUnit);
            RefreshDocumentStatus();
            return documentUnit;
        }

        // A competência que uma nova unidade teria, usada só para achar uma pendente equivalente antes de criar
        // outra. Precisa espelhar exatamente o que DocumentUnit.Create faz: sem data -> mínima; com data ->
        // corrente ou anterior conforme usePreviousPeriod.
        private static Period CandidatePeriodFor(PeriodType periodType, bool usePreviousPeriod, DateTime? referenceDate)
        {
            if (!referenceDate.HasValue)
                return Period.CreateMinimum(periodType);

            return usePreviousPeriod
                ? Period.CreatePreviousPeriod(periodType, referenceDate.Value)
                : Period.Create(periodType, referenceDate.Value);
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

        public DocumentUnit UpdateDocumentUnitDetails(Guid documentUnitId, DateOnly date, DateOnly? validity, string content,
            PeriodType? periodType = null, bool usePreviousPeriod = false)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.UpdateDetails(date, validity, content, periodType, usePreviousPeriod);

            if (documentUnit.IsPeriod)
                VerifyDuplicatedPendings(documentUnit);

            RefreshDocumentStatus();
            return documentUnit;
        }

        public DocumentUnit UpdateDocumentUnitDetails(Guid documentUnitId, DateOnly date, TimeSpan? validity, string content,
            PeriodType? periodType = null, bool usePreviousPeriod = false)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.UpdateDetails(date, validity, content, periodType, usePreviousPeriod);

            if (documentUnit.IsPeriod)
                VerifyDuplicatedPendings(documentUnit);

            RefreshDocumentStatus();
            return documentUnit;
        }

        private void VerifyDuplicatedPendings(DocumentUnit documentUnit)
        {
            var duplicatePending = DocumentsUnits
                .Where(x => x.Id != documentUnit.Id && x.Status == DocumentUnitStatus.Pending && x.Period != null && x.Period.Equals(documentUnit.Period))
                .ToList();

            duplicatePending.ForEach(x => x.MaskAsInvalid());
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

        public bool MarkAsNotApplicableDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            var isNotApplicable = document.MarkAsNotApplicable();
            if (isNotApplicable)
            {
                DeprecateDocumentsUnit(exceptionDocumentId: documentUnitId);
                RefreshDocumentStatus();
                return true;
            }
            return false;
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
            if (DocumentsUnits.Any(x => x.Period != null))
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



            if (DocumentsUnits.Any(x=> x.Period != null))
            {
                var periodStatuses = DocumentsUnits
                    .GroupBy(x => x.Period)
                    .Select(g => GetStatusFromGroup(g));
                if (periodStatuses.Any(x => x == DocumentStatus.RequiresDocument))
                    Status = DocumentStatus.RequiresDocument;
                else if(periodStatuses.Any(x => x == DocumentStatus.RequiresValidation))
                    Status = DocumentStatus.RequiresValidation;
                else if (periodStatuses.Any(x => x == DocumentStatus.Warning))
                    Status = DocumentStatus.Warning;    
                else 
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
