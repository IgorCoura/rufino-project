namespace PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate
{
    public class RequireSecurityDocuments : Entity, IAggregateRoot
    {        
        public Guid RoleId { get; private set; }
        public Guid CompanyId { get; private set; }
        public List<DocumentType> Types { get; private set; }

        private RequireSecurityDocuments(Guid id, Guid roleId, Guid companyId, List<DocumentType> types) : base(id)
        {
            RoleId = roleId;
            CompanyId = companyId;
            Types = types;
        }

        public RequireSecurityDocuments Create(Guid id, Guid roleId, Guid companyId, params DocumentType[] types) => new(id, roleId, companyId, [.. types]);
    }
}
