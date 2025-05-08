using System.Collections.Generic;

namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate
{
    public class RequireDocuments : Entity, IAggregateRoot
    {
        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;
        public AssociationType AssociationType { get; private set; } = null!;
        public Guid AssociationId { get; private set; } 
        public Guid CompanyId { get; private set; } 
        public List<Guid> DocumentsTemplatesIds { get; private set; } = [];
        public List<ListenEvent> ListenEvents { get; private set; } = [];

        private RequireDocuments() {}
        private RequireDocuments(Guid id, Guid companyId, Guid associationId, AssociationType associationType, Name name, 
            Description description, List<ListenEvent> listenEvents, List<Guid> documentsTemplatesIds) : base(id)
        {
            AssociationType = associationType;
            AssociationId = associationId;
            CompanyId = companyId;
            DocumentsTemplatesIds = documentsTemplatesIds;
            Name = name;
            Description = description;
            ListenEvents = listenEvents;
        }

        public static RequireDocuments Create(Guid id, Guid companyId, Guid associationId, AssociationType associationType, 
            Name name, Description description, List<ListenEvent> listenEvents, List<Guid> documentsTemplatesIds) 
            => new(id, companyId, associationId, associationType, name, description, listenEvents, documentsTemplatesIds);

        public void Edit(Guid id, Guid companyId, Guid associationId, AssociationType associationType,
            Name name, Description description, List<ListenEvent> listenEvents, List<Guid> documentsTemplatesIds)
        {
            Id = id;
            CompanyId = companyId;
            AssociationId = associationId;
            AssociationType = AssociationType;
            Name = name;
            Description = description;
            ListenEvents = listenEvents;
            DocumentsTemplatesIds = documentsTemplatesIds;
        }


        public bool StatusIsAccepted(int eventId, int statusId)
        {
            var listenEvent = ListenEvents.Find(x => x.EventId == eventId);

            return listenEvent!.Status.Contains(statusId);
        }
    }
}
