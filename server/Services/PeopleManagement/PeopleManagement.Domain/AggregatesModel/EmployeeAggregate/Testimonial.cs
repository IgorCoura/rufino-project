using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Testimonial : ValueObject
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

        private Testimonial(Guid archiveId)
        {
            ArchiveId = archiveId;
        }

        public Testimonial Create(Guid archiveId) => new(archiveId);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return ArchiveId;
        }
    }
}
