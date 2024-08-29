using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class DocumentUnit : Entity
    {
        private DateTime? _validity;
        public string Content { get; private set; } = null!;
        public DateTime? Validity 
        { 
            get => _validity;
            private set
            {
                if (value != null && Validity > DateTime.UtcNow)
                {
                    throw new DomainException(this, DomainErrors.DataIsGreaterThanMax(nameof(Validity), (DateTime)value, DateTime.Now));
                }
                _validity = value; 
            }
        }
        public Name? Name { get; private set; } = null!;
        public Extension? Extension { get; private set; } = null!;
        public DocumentUnitStatus Status { get; private set; } = DocumentUnitStatus.Pending;
        public DateTime Date { get; private set; }
        public Guid DocumentId { get; private set; }
        public Document Document { get; private set; } = null!;

        private DocumentUnit() { }
        private DocumentUnit(Guid id, string content, DateTime date, Document securityDocument, DateTime? validity) : base(id)
        {
            Content = content;
            Date = date;
            Document = securityDocument;
            DocumentId = securityDocument.Id;
            Validity = validity;
        }

        public static DocumentUnit Create(Guid id, string content, DateTime date, Document securityDocument, TimeSpan? validityDuration)
        {
            DateTime? validity = null;
            if(validityDuration != null)
                validity = date.Add((TimeSpan)validityDuration);

            return new(id, content, date, securityDocument, validity);
        }

        public void InsertWithRequireValidation(Name name, Extension extension)
        {
            Name = name;
            Extension = extension;
            Status = DocumentUnitStatus.RequiredValidaty;
        }
        public void InsertWithoutRequireValidation(Name name, Extension extension)
        {
            Name = name;
            Extension = extension;
            Status = DocumentUnitStatus.OK;
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
            if(Status == DocumentUnitStatus.OK)
                Status = DocumentUnitStatus.Deprecated;
        }

        public void NotApplicable()
        {
            if (Status == DocumentUnitStatus.Pending)
                Status = DocumentUnitStatus.NotApplicable;
        }

        public void AwaitingSignature()
        {
            if (Status == DocumentUnitStatus.Pending)
                Status = DocumentUnitStatus.AwaitingSignature;
        }
        public bool RequiresVerification => Status == DocumentUnitStatus.RequiredValidaty;
        public bool IsOK => Status == DocumentUnitStatus.OK;
        public string GetNameWithExtension => $"{Name}.{Extension}";
    }
}
