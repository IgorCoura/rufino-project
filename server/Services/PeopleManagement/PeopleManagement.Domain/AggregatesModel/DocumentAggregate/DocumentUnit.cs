using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                        throw new DomainException(this, DomainErrors.DataIsGreaterThanMax(nameof(Validity), (DateOnly)value, DateOnly.FromDateTime(DateTime.UtcNow)));
                }
                _validity = value; 
            }
        }

        public Name? Name { get; private set; } = null!;
        public Extension? Extension { get; private set; } = null!;
        public DocumentUnitStatus Status { get; private set; } = DocumentUnitStatus.Pending;
        public DateOnly Date { get; private set; }
        public Guid DocumentId { get; private set; }
        public Document Document { get; private set; } = null!;

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

        public void InsertWithRequireValidation(Name name, Extension extension)
        {
            if (IsInvalidDateAndValidity)
                throw new DomainException(this, DomainErrors.DataInvalid(nameof(Date), Date));
            Name = name;
            Extension = extension;
            Status = DocumentUnitStatus.RequiresValidation;
        }
        public void InsertWithoutRequireValidation(Name name, Extension extension)
        {
            if (IsInvalidDateAndValidity)
                throw new DomainException(this, DomainErrors.DataInvalid(nameof(Date), Date));
            Name = name;
            Extension = extension;
            Status = DocumentUnitStatus.OK;
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
            if (validity is not null)
            {
                var dateTimeValidity = date.ToDateTime(TimeOnly.MinValue).Add(validity.Value);
                dateValidity = DateOnly.FromDateTime(dateTimeValidity);
            }
            Validity = dateValidity;
            Content = content;
        }

        public void Validate(bool IsValid)
        {
            if(IsValid && Name != null && Extension != null)
            {
                Status = DocumentUnitStatus.OK;
            }
            Status = DocumentUnitStatus.Invalid;
        }

        public void Deprecate()
        {
            if (Status == DocumentUnitStatus.OK ||
                Status == DocumentUnitStatus.RequiresValidation ||
                Status == DocumentUnitStatus.AwaitingSignature || 
                Status == DocumentUnitStatus.Pending)
            {
                Status = DocumentUnitStatus.Deprecated;
            }
        }

        public void MarkAsNotApplicable()
        {
            if (Status == DocumentUnitStatus.Pending)
                Status = DocumentUnitStatus.NotApplicable;
        }

        public void MarkAsAwaitingSignature()
        {
            if (IsInvalidDateAndValidity)
                throw new DomainException(this, DomainErrors.DataInvalid(nameof(Date), Date));

            if (Status == DocumentUnitStatus.Pending)
                Status = DocumentUnitStatus.AwaitingSignature;
        }

        public bool HasContent => string.IsNullOrEmpty(Content) == false;

        public bool IsAwaitingSignature => Status == DocumentUnitStatus.AwaitingSignature;
        public bool RequiresVerification => Status == DocumentUnitStatus.RequiresValidation;
        public bool IsOK => Status == DocumentUnitStatus.OK;
        public bool IsPending => Status == DocumentUnitStatus.Pending;
        public string GetNameWithExtension => $"{Name}.{Extension}";
        public bool CanEdit => (Name == null || Name.IsNullOrEmpty) && Extension == null;

        private bool IsInvalidDateAndValidity => Date == DateOnly.MinValue || Date == DateOnly.MaxValue || Validity == null || Validity == DateOnly.MinValue || Validity == DateOnly.MaxValue;
        

    }
}
