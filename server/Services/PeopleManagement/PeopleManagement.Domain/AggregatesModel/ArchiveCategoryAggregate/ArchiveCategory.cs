#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
namespace PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
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
