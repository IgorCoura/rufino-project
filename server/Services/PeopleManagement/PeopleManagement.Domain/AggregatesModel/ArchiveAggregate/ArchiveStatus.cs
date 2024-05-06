namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public class ArchiveStatus : Enumeration
    {
        public static readonly ArchiveStatus OK = new(1, nameof(OK));
        public static readonly ArchiveStatus RequiresFile = new (2, nameof(RequiresFile));
        public static readonly ArchiveStatus RequiresVerification = new (3, nameof(RequiresVerification));

        private ArchiveStatus(int id, string name) : base(id, name)
        {
        }
    }
}
