using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
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

        private RequireDocuments() { }
        private RequireDocuments(Guid id, Guid companyId, Guid associationId, AssociationType associationType, Name name,
            Description description, List<ListenEvent> listenEvents, List<Guid> documentsTemplatesIds) : base(id)
        {
            ListenEventsIsValid(listenEvents);
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

        public void Edit(Guid associationId, AssociationType associationType,
            Name name, Description description, List<ListenEvent> listenEvents, List<Guid> documentsTemplatesIds)
        {
            ListenEventsIsValid(listenEvents);

            AssociationId = associationId;
            AssociationType = associationType;
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

        public static bool EventIsValid(int eventId)
        {
            if (EmployeeEvent.EventExist(eventId))
                return true;

            if (RecurringEvents.EventExist(eventId))
                return true;

            return false;
        }

        public void ListenEventsIsValid(List<ListenEvent> listenEvents)
        {
            var invalidEvents = GetInvalidEventId(listenEvents);
            if (invalidEvents.Length > 0)
                throw new DomainException(this, DomainErrors.FieldInvalid(nameof(ListenEvent), ListenEvent.ConvertArrayToString(invalidEvents)));

        }

        public static ListenEvent[] GetInvalidEventId(List<ListenEvent> listenEvents)
        {
            var result = listenEvents.Where(x => !EventIsValid(x.EventId)) ?? [];
            return result.ToArray();
        }

        public static string GetEventName(int id)
        {
            var employeeEvent = EmployeeEvent.FromValue(id);

            if (employeeEvent is not null)
                return employeeEvent.Name;

            var recurringEvents = RecurringEvents.FromValue(id);

            return recurringEvents?.Name ?? "";
        }
    }
}
