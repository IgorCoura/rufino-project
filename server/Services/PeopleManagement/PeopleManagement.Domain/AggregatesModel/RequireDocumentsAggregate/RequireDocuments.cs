using System.Collections.Generic;

namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate
{
    public class RequireDocuments : Entity, IAggregateRoot
    {
        public Name Name { get; private set; }
        public Description Description { get; private set; }
        public AssociationType AssociationType { get; private set; }
        public Guid AssociationId { get; private set; }
        public Guid CompanyId { get; private set; }
        public List<Guid> DocumentsTemplatesIds { get; private set; }
        public List<int> ListenEventsIds { get; private set; } = [];

        private RequireDocuments(Guid id, Guid companyId, Guid associationId, AssociationType associationType, Name name, Description description, List<int> listenEventsIds, List<Guid> documentsTemplatesIds) : base(id)
        {
            AssociationType = associationType;
            AssociationId = associationId;
            CompanyId = companyId;
            DocumentsTemplatesIds = documentsTemplatesIds;
            Name = name;
            Description = description;
            ListenEventsIds = listenEventsIds;
        }

        public static RequireDocuments Create(Guid id, Guid companyId, Guid associationId, AssociationType associationType, Name name, Description description, List<int> listenEventsIds, List<Guid> documentsTemplatesIds) => new(id, companyId, associationId, associationType, name, description, listenEventsIds, documentsTemplatesIds);
    }
}
