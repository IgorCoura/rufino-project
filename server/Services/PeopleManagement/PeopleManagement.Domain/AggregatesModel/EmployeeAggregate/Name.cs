using PeopleManagement.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Name : ValueObject
    {
        const int MAX_LENGTH = 100;

        private string _value = string.Empty;

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(_value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(this.GetType().Name));

                if (_value.Length < MAX_LENGTH)
                    throw new DomainException(DomainErrors.FieldInvalid(this.GetType().Name, value));

                if (!Regex.IsMatch(_value, (@"[^a-zA-Z0-9]")))
                    throw new DomainException(DomainErrors.FieldCannotHaveSpecialChar(this.GetType().Name));

                _value = value.ToUpper();
            }
        }

        private Name(){
        }

        private Name(string value)
        {
            Value = value;
        }

        public static implicit operator Name(string value) =>
            new(value);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
