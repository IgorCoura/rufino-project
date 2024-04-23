
using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Picture : ValueObject
    {
        private Guid _archiveId;
        public Guid ArchiveId 
        {
            get => _archiveId;
            private set
            {
                if (value == Guid.Empty)
                    throw new DomainException(DomainErrors.ObjectNotBeDefaultValue(nameof(ArchiveId), Guid.Empty.ToString()));
                _archiveId = value;
            }
        }

        public Picture(Guid archiveId)
        {
            ArchiveId = archiveId;
        }

        public static implicit operator Picture(Guid input) => new(input);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            throw new NotImplementedException();
        }
    }
}
