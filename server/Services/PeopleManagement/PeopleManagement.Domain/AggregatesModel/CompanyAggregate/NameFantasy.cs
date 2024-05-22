using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.CompanyAggregate
{
    public sealed class NameFantasy : ValueObject
    {
        public const int MAX_LENGTH = 100;

        private string _value = string.Empty;

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                value = value.ToUpper().Trim();

                if (String.IsNullOrWhiteSpace(value))
                    throw new DomainException(this, DomainErrors.FieldNotBeNullOrEmpty(this.GetType().Name));

                if (value.Length > MAX_LENGTH)
                    throw new DomainException(this, DomainErrors.FieldInvalid(this.GetType().Name, value));


                _value = value;
            }
        }

        private NameFantasy(){
        }

        private NameFantasy(string value)
        {
            Value = value;
        }

        public static implicit operator NameFantasy(string value) =>
            new(value);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

    }
}
