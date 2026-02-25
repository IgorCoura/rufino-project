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

        private DocumentUnit() { }
        private DocumentUnit(Guid id, Document document) : base(id)
        {
            Document = document;
            DocumentId = document.Id;
        }

        public static DocumentUnit Create(Guid id,  Document document)
        {
            return new(id,  document);
        }

        public static DocumentUnit Create(Guid id, Document document, PeriodType? periodType, DateTime? referenceDate)
        {
            var documentUnit = new DocumentUnit(id, document);

            if (periodType is not null && referenceDate.HasValue)
            {
                documentUnit.Period = document.UsePreviousPeriod
                    ? Period.CreatePreviousPeriod(periodType, referenceDate.Value)
                    : Period.Create(periodType, referenceDate.Value);
                DateOnly? validity = null;
                documentUnit.UpdateDetails(DateOnly.FromDateTime((DateTime)referenceDate), validity, "");
            }

            return documentUnit;
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
                AddDomainEvent(ScheduleDocumentExpirationEvent.Create(Document.Id, Id, Document.CompanyId, (DateOnly)Validity));
        }

        public void UpdateDetails(DateOnly date, DateOnly? validity, string content)
        {
            Date = date;
            Validity = validity;    
            Content = content;
        }

        public void UpdateDetails(DateOnly date, TimeSpan? validity, string content)
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
                    AddDomainEvent(ScheduleDocumentExpirationEvent.Create(Document.Id, Id, Document.CompanyId, (DateOnly)Validity!));
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

            if (Status == DocumentUnitStatus.Pending)
            {
                
                Status = DocumentUnitStatus.AwaitingSignature;
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

        private bool HasInvalidDateOrValidity => Date == DateOnly.MinValue || Date == DateOnly.MaxValue || (Validity != null && (Validity == DateOnly.MinValue || Validity == DateOnly.MaxValue));

        public void SetSignatureInfo(string documentToken, string signatureUrl)
        {
            SignatureDocumentToken = documentToken;
            SignatureUrl = signatureUrl;
        }

    }
}
