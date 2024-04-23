using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class VoteId : ValueObject
    {
        const int MAX_LENGHT = 12;

        private string _number = string.Empty;
        private Guid _archiveId;

        public string Number
        {
            get => _number;
            set
            {
                value = value.Trim();
                value = value.Replace(".", "").Replace("-", "").Replace("/", "").Replace(" ", "");
                Validate(value);
                _number = value;
            }
        }

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


        private VoteId(string value, Guid archiveId)
        {
            Number = value;
            ArchiveId = archiveId;
        }


        public static VoteId Create(string value, Guid archiveId) => new(value, archiveId);

        private void Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(VoteId)));
            }

            int[] multiplierOne = new int[8] { 2, 3, 4, 5, 6, 7, 8, 9 };
            int[] multiplierTwo = new int[3] { 7, 8, 9 };
            var invalido = new List<string> { "000000000000", "111111111111", "222222222222", "333333333333", "444444444444", "555555555555", "666666666666", "777777777777", "888888888888", "999999999999" };
            string aux;
            string digit;
            int sum, rest;
            if (value.Length != MAX_LENGHT)
                throw new DomainException((DomainErrors.FieldCannotBeLarger(nameof(VoteId), MAX_LENGHT)));

            foreach (string item in invalido)
            {
                if (item == value)
                    throw new DomainException(DomainErrors.FieldInvalid(nameof(VoteId), value));
            }

            aux = value[..8];
            sum = 0;

            for (int i = 0; i < 8; i++)
                sum += int.Parse(aux[i].ToString()) * multiplierOne[i];

            rest = sum % 11;

            if (rest > 9)
                rest = 0;

            digit = rest.ToString();

            aux = $"{value[8]}{value[9]}{digit}";
            sum = 0;

            for (int i = 0; i < 3; i++)
                sum += int.Parse(aux[i].ToString()) * multiplierTwo[i];

            rest = sum % 11;

            if (rest > 9)
                rest = 0;

            digit += rest.ToString();

            if (!value.EndsWith(digit))
                throw new DomainException(DomainErrors.FieldInvalid(nameof(VoteId), value));
        }

        public override string ToString() => Number;
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Number;
        }
    }
}
