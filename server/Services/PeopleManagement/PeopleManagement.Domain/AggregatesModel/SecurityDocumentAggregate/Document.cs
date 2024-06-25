using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate
{
    public class Document : Entity
    {
        private DateTime? _validity;
        public string Template { get; private set; } = string.Empty;
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
        public Name? Name { get; private set; }
        public Extension? Extension { get; private set; } 
        public DocumentStatus Status { get; private set; } = DocumentStatus.Pending;
        public DateTime Date { get; private set; } 

        private Document(Guid id, string template, DateTime date) : base(id)
        {
            Template = template;
            Date = date;
        }

        public static Document Create(Guid id, string template, DateTime date) => new(id, template, date);

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
