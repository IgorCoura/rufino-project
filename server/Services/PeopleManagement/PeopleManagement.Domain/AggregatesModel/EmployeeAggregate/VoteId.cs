using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class VoteId : ValueObject
    {
        public const int MAX_LENGHT = 12;

        private string _number = string.Empty;
        public string Number
        {
            get => _number;
            set
            {
                var temp = value.Select(x => char.IsDigit(x) ? x : ' ').ToArray();
                value = new string(temp).Replace(" ", "");

                Validate(value);
                _number = value;
            }
        }

        private VoteId(string value)
        {
            Number = value;
        }


        public static VoteId Create(string value) => new(value);


        private void Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(VoteId)));
            }

            int[] multiplierOne = [ 2, 3, 4, 5, 6, 7, 8, 9 ];
            int[] multiplierTwo = [ 7, 8, 9 ];
            var invalido = new List<string> { "000000000000", "111111111111", "222222222222", "333333333333", "444444444444", "555555555555", "666666666666", "777777777777", "888888888888", "999999999999" };
            string aux;
            string digit;
            int sum, rest;
            if (value.Length != MAX_LENGHT)
                throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(VoteId), MAX_LENGHT));

            foreach (string item in invalido)
            {
                if (item == value)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldInvalid(nameof(VoteId), value));
            }

            string uf = value[8..10];
            aux = value[..8];
            sum = 0;

            for (int i = 0; i < 8; i++)
                sum += int.Parse(aux[i].ToString()) * multiplierOne[i];

            rest = sum % 11;

            if (rest > 9)
                rest = 0;
            else if (rest == 0)
            {
                if(uf == "01" || uf == "02")
                {
                    rest = 1;
                }
            }

            digit = rest.ToString();

            aux = $"{value[8]}{value[9]}{digit}";
            sum = 0;

            for (int i = 0; i < 3; i++)
                sum += int.Parse(aux[i].ToString()) * multiplierTwo[i];

            rest = sum % 11;

            if (rest > 9)
                rest = 0;
            else if (rest == 0)
            {
                if (uf == "01" || uf == "02")
                {
                    rest = 1;
                }
            }


            digit += rest.ToString();

            if (!value.EndsWith(digit))
                throw new DomainException(this.GetType().Name, DomainErrors.FieldInvalid(nameof(VoteId), value));
        }

        public override string ToString() => Number;

        public static implicit operator VoteId(string number) => new(number);
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Number;
        }
    }
}
