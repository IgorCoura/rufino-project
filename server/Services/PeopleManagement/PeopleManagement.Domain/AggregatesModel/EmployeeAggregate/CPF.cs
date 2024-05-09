using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class CPF : ValueObject
    {
        const int MAX_LENGHT = 11;

        private string _number = string.Empty;
        public string Number 
        {   
            get => _number;
            private set
            {
                var temp = value.Select(x => char.IsDigit(x) ? x : ' ').ToArray();
                value = new string(temp).Replace(" ", "");
                Validate(value);
                _number = value;
            }
        }

        private CPF(string value)
        {
            Number = value;
        }

        private void Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(CPF)));
            }

            int[] multiplierOne = [ 10, 9, 8, 7, 6, 5, 4, 3, 2 ];
            int[] multiplierTwo = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2 ];
            string[] cpfInvalido = [ "00000000000", "11111111111", "22222222222", "33333333333", "44444444444", "55555555555", "66666666666", "77777777777", "88888888888", "99999999999" ];
            string aux;
            string digit;
            int sum, rest;

            
            if (value.Length != MAX_LENGHT)
            {
                throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(CPF), MAX_LENGHT));
            }

            foreach (string item in cpfInvalido)
            {
                if (item == value)
                {
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldInvalid(nameof(CPF), value));
                }
            }

            aux = value[..9];
            sum = 0;

            for (int i = 0; i < 9; i++)
                sum += int.Parse(aux[i].ToString()) * multiplierOne[i];

            rest = sum % 11;

            if (rest < 2)
                rest = 0;
            else
                rest = 11 - rest;

            digit = rest.ToString();
            aux += digit;
            sum = 0;

            for (int i = 0; i < 10; i++)
                sum += int.Parse(aux[i].ToString()) * multiplierTwo[i];

            rest = sum % 11;

            if (rest < 2)
                rest = 0;
            else
                rest = 11 - rest;

            digit += rest.ToString();

            if (!value.EndsWith(digit))
                throw new DomainException(this.GetType().Name, DomainErrors.FieldInvalid(nameof(CPF), value));
        }

        public override string ToString() => Number;

        public static implicit operator CPF(string input) =>
            new(input);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Number;
        }
    }
}
