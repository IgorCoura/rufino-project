using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;

namespace PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate
{
    public class DocumentType : Enumeration
    {
        public readonly DocumentType NR01 = new(1, nameof(NR01), TimeSpan.FromDays(375));

        public TimeSpan? ValidityInterval { get; private set; }
        private DocumentType(int id, string name, TimeSpan? validityInterval = null) : base(id, name)
        {
            ValidityInterval = validityInterval;
        }

        public static implicit operator DocumentType(int id) => Enumeration.FromValue<DocumentType>(id);
        public static implicit operator DocumentType(string name) => Enumeration.FromDisplayName<DocumentType>(name);
    }
}
