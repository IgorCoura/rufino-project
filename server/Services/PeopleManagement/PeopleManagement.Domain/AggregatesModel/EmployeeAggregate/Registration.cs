using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Registration : ValueObject
    {
        public const int MAX_CHARS_REGISTRATION = 15;
        private string _value = string.Empty;       
        public string Value
        {
            get => _value;
            private set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Registration)));

                if (value.Length > MAX_CHARS_REGISTRATION)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Registration), MAX_CHARS_REGISTRATION));

                _value = value;
            }
        }

        private Registration(string value)
        {
            Value = value;
        }

        public static Registration Create(string value) => new(value);


        public static implicit operator Registration(string input) =>
            new(input);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
