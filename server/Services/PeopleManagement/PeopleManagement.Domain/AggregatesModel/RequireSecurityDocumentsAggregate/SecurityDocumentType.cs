namespace PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate
{
    public class SecurityDocumentType : Enumeration
    {
        public static SecurityDocumentType NR01 = new(1, nameof(NR01));
        private SecurityDocumentType(int id, string name) : base(id, name)
        {
        }

        public static implicit operator SecurityDocumentType(int id) => Enumeration.FromValue<SecurityDocumentType>(id);
        public static implicit operator SecurityDocumentType(string name) => Enumeration.FromDisplayName<SecurityDocumentType>(name);
    }
}
