using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate
{
    public class Document : Entity
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
        public DocumentStatus Status { get; private set; } = DocumentStatus.Pending;
        public DateTime Date { get; private set; }
        public Guid SecurityDocumentId { get; private set; }
        public SecurityDocument SecurityDocument { get; private set; } = null!;

        private Document() { }
        private Document(Guid id, string content, DateTime date, SecurityDocument securityDocument) : base(id)
        {
            Content = content;
            Date = date;
            SecurityDocument = securityDocument;
            SecurityDocumentId = securityDocument.Id;
        }

        public static Document Create(Guid id, string content, DateTime date, SecurityDocument securityDocument) => new(id, content, date, securityDocument);

        public void InsertFileWithRequireValidation(Name name, Extension extension, DateTime? validity)
        {
            Name = name;
            Extension = Extension;
            Validity = validity;
            Status = DocumentStatus.RequiredValidaty;
        }
        public void InsertFileWithoutRequireValidation(Name name, Extension extension, DateTime? validity)
        {
            Name = name;
            Extension = Extension;
            Validity = validity;
            Status = DocumentStatus.OK;
        }

        public void Validate(bool IsValid)
        {
            if(IsValid && Name != null && Extension != null)
            {
                Status = DocumentStatus.OK;
            }
            Status = DocumentStatus.Invalid;
        }

        public void Deprecate()
        {
            if(Status == DocumentStatus.OK)
                Status = DocumentStatus.Deprecated;
        }

        public bool RequiresVerification => Status == DocumentStatus.RequiredValidaty;
        public bool IsOK => Status == DocumentStatus.OK;
        public string GetNameWithExtension => $"{Name}.{Extension}";
    }
}
