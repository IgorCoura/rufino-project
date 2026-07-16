using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class DocumentUnit : Entity
    {
        private DateOnly? _validity;
        public string Content { get; private set; } = string.Empty;
        public DateOnly? Validity 
        { 
            get => _validity;
            private set
            {
                
                if (value != null)
                {
                    DateOnly cValue = (DateOnly)value;
                    if(cValue.ToDateTime(TimeOnly.MinValue) < DateTime.UtcNow)
                        throw new DomainException(this, DomainErrors.DataIsGreaterThanMax(nameof(Validity), 
                            (DateOnly)value, DateOnly.FromDateTime(DateTime.UtcNow)));
                }
                _validity = value;               
            }
        }

        public Name? Name { get; private set; } = null!;
        public Extension? Extension { get; private set; } = null!;
        public Period? Period { get; private set; }
        public DocumentUnitStatus Status { get; private set; } = DocumentUnitStatus.Pending;
        public DateOnly Date { get; private set; }
        public Guid DocumentId { get; private set; }
        public Document Document { get; private set; } = null!;
        public string? SignatureDocumentToken { get; private set; }
        public string? SignatureUrl { get; private set; }
        public string? AttachmentToken { get; private set; }
        public DateTime? SentToSignatureAt { get; private set; }
        public DateOnly? WorkloadEndDate { get; private set; }

        private DocumentUnit() { }
        private DocumentUnit(Guid id, Document document) : base(id)
        {
            Document = document;
            DocumentId = document.Id;
        }

        /// <summary>
        /// Cria a unidade situando-a na competência configurada no template do documento, quando houver.
        ///
        /// A configuração ([periodType]/[usePreviousPeriod]) vem do template, lida pelo caller no momento da
        /// operação — nem o documento nem a unidade guardam cópia da regra; a unidade guarda só a competência em
        /// que caiu (a história). Com [referenceDate], a unidade cai na competência daquela data (e a data vira o
        /// Date da unidade). Sem [referenceDate], a unidade ainda não tem data que a situe, então recebe a
        /// competência mínima possível — substituída assim que uma data real chega por UpdateDetails.
        /// </summary>
        public static DocumentUnit Create(Guid id, Document document, PeriodType? periodType = null, bool usePreviousPeriod = false, DateTime? referenceDate = null)
        {
            var documentUnit = new DocumentUnit(id, document);

            if (periodType is not null)
            {
                if (referenceDate.HasValue)
                {
                    documentUnit.UpdateDetails(DateOnly.FromDateTime(referenceDate.Value), (DateOnly?)null, "", periodType, usePreviousPeriod);
                }
                else
                {
                    documentUnit.Period = Period.CreateMinimum(periodType);
                }
            }

            return documentUnit;
        }

        /// <summary>
        /// Recalcula a competência da unidade a partir de [referenceDate], com a configuração atual do template
        /// (lida pelo caller no momento da operação).
        /// </summary>
        public void SetPeriod(DateTime referenceDate, PeriodType periodType, bool usePreviousPeriod)
        {
            Period = usePreviousPeriod
                ? Period.CreatePreviousPeriod(periodType, referenceDate)
                : Period.Create(periodType, referenceDate);
        }

        /// <summary>
        /// Re-situa a unidade na competência mínima de [periodType]. Usado quando uma pendente que espera data é
        /// reaproveitada após o template trocar de granularidade — a mínima antiga não pode sobreviver com o tipo
        /// velho, senão a próxima busca por pendente equivalente não a encontraria.
        /// </summary>
        public void ResetPeriodToMinimum(PeriodType periodType)
        {
            Period = Period.CreateMinimum(periodType);
        }

        public void InsertWithRequireValidation(Name name, Extension extension)
        {
            if (HasInvalidDateOrValidity)
                throw new DomainException(this, DomainErrors.DataInvalid(nameof(Date), Date));
            Name = name;
            Extension = extension;
            Status = DocumentUnitStatus.RequiresValidation;
        }
        public void InsertWithoutRequireValidation(Name name, Extension extension)
        {
            if (HasInvalidDateOrValidity)
                throw new DomainException(this, DomainErrors.DataInvalid(nameof(Date), Date));
            Name = name;
            Extension = extension;
            Status = DocumentUnitStatus.OK;
            if(Validity is not null)
                AddDomainEvent(ScheduleDocumentExpirationEvent.Create(Document.Id, Id, Document.CompanyId, (DateOnly)Validity, Date));
        }

        // Com [periodType], a data recebida re-situa a unidade na competência correspondente — inclusive quando a
        // unidade ainda não tinha competência (documento nascido antes de o template ganhar a PeriodPolicy passa
        // a ser situado aqui). Sem [periodType] (template sem a regra), a competência existente fica intocada:
        // ela é história, não configuração.
        public void UpdateDetails(DateOnly date, DateOnly? validity, string content, PeriodType? periodType = null, bool usePreviousPeriod = false)
        {
            Date = date;
            Validity = validity;
            Content = content;
            if (periodType is not null)
                SetPeriod(date.ToDateTime(TimeOnly.MinValue), periodType, usePreviousPeriod);
        }

        public void UpdateDetails(DateOnly date, TimeSpan? validity, string content, PeriodType? periodType = null, bool usePreviousPeriod = false)
        {
            Date = date;
            DateOnly? dateValidity = null;
            if (validity is not null && validity != TimeSpan.Zero)
            {
                var dateTimeValidity = date.ToDateTime(TimeOnly.MinValue).Add(validity.Value);
                dateValidity = DateOnly.FromDateTime(dateTimeValidity);
            }
            Validity = dateValidity;
            Content = content;
            if (periodType is not null)
                SetPeriod(date.ToDateTime(TimeOnly.MinValue), periodType, usePreviousPeriod);
        }



        public void MaskAsInvalid()
        {
            Status = DocumentUnitStatus.Invalid;
        }

        public void MaskAsValid()
        {
            if (Name != null && Extension != null)
            {
                Status = DocumentUnitStatus.OK;
                if (Validity is not null)
                    AddDomainEvent(ScheduleDocumentExpirationEvent.Create(Document.Id, Id, Document.CompanyId, (DateOnly)Validity!, Date));
            }
            else
            {
                throw new DomainException(this, DomainErrors.Document.DocumentUnitMissingNameOrExtension(Id));
            }
        }

        public bool MarkAsDeprecatedOrInvalid()
        {
            if (Status == DocumentUnitStatus.OK || Status == DocumentUnitStatus.Warning)
            {
                Status = DocumentUnitStatus.Deprecated;
                return true;
            }
            if(Status == DocumentUnitStatus.RequiresValidation ||
                Status == DocumentUnitStatus.AwaitingSignature ||
                Status == DocumentUnitStatus.Pending)
            {
                Status = DocumentUnitStatus.Invalid;
                return true;
            }
            return false;
        }

        public bool MarkAsNotApplicable()
        {
            if (Status == DocumentUnitStatus.Pending)
            {
                Status = DocumentUnitStatus.NotApplicable;
                return true;
            }
                
            return false;
        }

        public bool MarkAsWarning()
        {
            if (Status == DocumentUnitStatus.OK)
            {
                Status = DocumentUnitStatus.Warning;
                return true;
            }
            return false;
        }


        public bool MarkAsAwaitingSignature()
        {
            if (HasInvalidDateOrValidity)
                throw new DomainException(this, DomainErrors.DataInvalid(nameof(Date), Date));

            if (Date > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new DomainException(this, DomainErrors.Document.DocumentUnitCantBeSentBeforeOfficialDate(Id, Date));

            if (Status == DocumentUnitStatus.Pending)
            {
                Status = DocumentUnitStatus.AwaitingSignature;
                SentToSignatureAt = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        public bool HasContent => string.IsNullOrEmpty(Content) == false;

        public bool IsAwaitingSignature => Status == DocumentUnitStatus.AwaitingSignature;
        public bool RequiresVerification => Status == DocumentUnitStatus.RequiresValidation;
        public bool IsOK => Status == DocumentUnitStatus.OK;
        public bool IsPending => Status == DocumentUnitStatus.Pending;
        public string GetNameWithExtension => $"{Name}.{Extension}";
        public bool CanEdit => (Name == null || Name.IsNullOrEmpty) && Extension == null;

        public bool IsPeriodDaily => Period?.IsDaily ?? false;
        public bool IsPeriodWeekly => Period?.IsWeekly ?? false;
        public bool IsPeriodMonthly => Period?.IsMonthly ?? false;
        public bool IsPeriodYearly => Period?.IsYearly ?? false;
        public bool IsPeriod => Period != null; 

        private bool HasInvalidDateOrValidity => Date == DateOnly.MinValue || Date == DateOnly.MaxValue || (Validity != null && (Validity == DateOnly.MinValue || Validity == DateOnly.MaxValue));

        public void SetSignatureInfo(string documentToken, string signatureUrl)
        {
            SignatureDocumentToken = documentToken;
            SignatureUrl = signatureUrl;
        }

        public void SetAttachmentSignatureInfo(string sessionDocToken, string attachmentToken, string signatureUrl)
        {
            SignatureDocumentToken = sessionDocToken;
            AttachmentToken = attachmentToken;
            SignatureUrl = signatureUrl;
        }

        public bool IsSessionPrimary => IsAwaitingSignature && SignatureDocumentToken != null && AttachmentToken == null;

        public void SetWorkloadEndDate(DateOnly endDate)
        {
            WorkloadEndDate = endDate;
        }

    }
}
