using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public sealed class ArchiveStatus : Enumeration
    {
        public static readonly ArchiveStatus OK = new(1, nameof(OK));
        public static readonly ArchiveStatus RequiresFile = new (2, nameof(RequiresFile));
        public static readonly ArchiveStatus RequiresVerification = new (3, nameof(RequiresVerification));        

        private ArchiveStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator ArchiveStatus(int id) => Enumeration.FromValue<ArchiveStatus>(id);
        public static implicit operator ArchiveStatus(string name) => Enumeration.FromDisplayName<ArchiveStatus>(name);
    }
}
