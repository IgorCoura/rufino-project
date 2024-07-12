namespace PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate
{
    public class RequireSecurityDocuments : Entity, IAggregateRoot
    {        
        public Guid RoleId { get; private set; }
        public Guid CompanyId { get; private set; }
        public List<Guid> DocumentsTemplatesIds { get; private set; }

        private RequireSecurityDocuments(Guid id, Guid roleId, Guid companyId, List<Guid> documentsTemplatesIds) : base(id)
        {
            RoleId = roleId;
            CompanyId = companyId;
            DocumentsTemplatesIds = documentsTemplatesIds;
        }

        public RequireSecurityDocuments Create(Guid id, Guid roleId, Guid companyId, params Guid[] documentsTemplatesIds) => new(id, roleId, companyId, [.. documentsTemplatesIds]);
    }
}
