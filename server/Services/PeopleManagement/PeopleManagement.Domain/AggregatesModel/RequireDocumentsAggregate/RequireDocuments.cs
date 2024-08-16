namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate
{
    public class RequireDocuments : Entity, IAggregateRoot
    {
        public Name Name { get; private set; }
        public Description Description { get; private set; }
        public Guid RoleId { get; private set; }
        public Guid CompanyId { get; private set; }
        public List<Guid> DocumentsTemplatesIds { get; private set; }

        private RequireDocuments(Guid id, Guid roleId, Guid companyId, List<Guid> documentsTemplatesIds, Name name, Description description) : base(id)
        {
            RoleId = roleId;
            CompanyId = companyId;
            DocumentsTemplatesIds = documentsTemplatesIds;
            Name = name;
            Description = description;
        }

        public static RequireDocuments Create(Guid id, Guid roleId, Guid companyId, Name name, Description description, params Guid[] documentsTemplatesIds) => new(id, roleId, companyId, [.. documentsTemplatesIds], name, description);
    }
}
