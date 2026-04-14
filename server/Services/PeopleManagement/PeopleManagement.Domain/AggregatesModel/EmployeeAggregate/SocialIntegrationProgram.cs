using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class SocialIntegrationProgram : ValueObject
    {
        public const int MAX_LENGHT = 11;

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

        private SocialIntegrationProgram(string value)
        {
            Number = value;
        }

        public static SocialIntegrationProgram Create(string value) => new(value);

        private void Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(SocialIntegrationProgram)));

            int[] multiplier = [3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            var invalido = new string[10] { "00000000000", "11111111111", "22222222222", "33333333333", "44444444444", "55555555555", "66666666666", "77777777777", "88888888888", "99999999999" };

            if (value.Length != MAX_LENGHT)
                throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(SocialIntegrationProgram), MAX_LENGHT));

            foreach (string item in invalido)
            {
                if (item == value)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldInvalid(nameof(SocialIntegrationProgram), value));
            }

            var aux = value[..10];
            var sum = 0;

            for (int i = 0; i < 10; i++)
                sum += int.Parse(aux[i].ToString()) * multiplier[i];

            var rest = 11 - (sum % 11);

            if (rest > 9)
                rest = 0;

            var digit = rest.ToString();

            if (!value.EndsWith(digit))
                throw new DomainException(this.GetType().Name, DomainErrors.FieldInvalid(nameof(SocialIntegrationProgram), value));
        }

        public override string ToString() => Number;

        public static implicit operator SocialIntegrationProgram(string input) => new(input);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Number;
        }
    }
}
