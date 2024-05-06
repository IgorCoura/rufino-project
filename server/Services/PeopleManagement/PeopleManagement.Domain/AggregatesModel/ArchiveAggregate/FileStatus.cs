namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public class FileStatus : Enumeration
    {
        public static readonly FileStatus Pending = new(1, nameof(Pending));
        public static readonly FileStatus OK = new(2, nameof(OK));
        public static readonly FileStatus Refused = new(3, nameof(Refused));
        private FileStatus(int id, string name) : base(id, name)
        {
        }
    }
}
