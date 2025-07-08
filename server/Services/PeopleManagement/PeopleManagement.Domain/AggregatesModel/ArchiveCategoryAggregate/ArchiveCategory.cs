namespace PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate
{
    public class ArchiveCategory : Entity, IAggregateRoot
    {
        public Name Name { get; set; } = null!;
        public Description Description { get; set; } = null!;
        public List<int> ListenEventsIds { get; private set; } = [];
        public Guid CompanyId { get; private set; }

        private ArchiveCategory() { }

        private ArchiveCategory(Guid id, Name name, Description description, List<int> listenEventsIds, Guid companyId) : base(id)
        {
            Name = name;
            Description = description;
            ListenEventsIds = listenEventsIds;
            CompanyId = companyId;
        } 

        public static ArchiveCategory Create(Guid id, Name name, Description description, List<int> listenEventsIds, Guid companyId) => new(id, name, description, listenEventsIds, companyId);

        public void RemoveListenEvent(int eventId)
        {
            ListenEventsIds.Remove(eventId);
        }

        public void RemoveRangeListenEvent(int[] eventId)
        {
            foreach (var item in eventId)
            {
                ListenEventsIds.Remove(item);
            }
        }


        public void AddListenEvent(int eventId)
        {
            ListenEventsIds.Add(eventId);
        }
    }
}
