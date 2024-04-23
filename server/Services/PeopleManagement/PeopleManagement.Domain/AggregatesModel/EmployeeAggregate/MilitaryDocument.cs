using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class MilitaryDocument : ValueObject
    {
        public const int MAX_NUMBER = 20;
        public const int MAX_TYPE = 50;

        private string? _number;
        private string? _type;
        private Guid? _archiveId;

        public string Number
        {
            get => _number ?? throw new ArgumentNullException();
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(Number)));
                if (value.Length > MAX_NUMBER)
                    throw new DomainException(DomainErrors.FieldCannotBeLarger(nameof(Number), MAX_NUMBER));

                _number = value;
            }
        }
        public string Type 
        { 
            get => _type ?? throw new ArgumentNullException();
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(Type)));
                if (value.Length > MAX_TYPE)
                    throw new DomainException(DomainErrors.FieldCannotBeLarger(nameof(Type), MAX_TYPE));

                _type = value;
            }
        }

        public Guid ArchiveId
        {
            get => _archiveId ?? throw new ArgumentNullException();
            private set => _archiveId = value;
        }


        private MilitaryDocument(string number, string type, Guid archiveId)
        {
            Number = number;
            Type = type;
            ArchiveId = archiveId;
        }

        public static MilitaryDocument Create(string number, string type, Guid archiveId) => new(number, type, archiveId);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Number;
            yield return Type;
        }
    }
}
