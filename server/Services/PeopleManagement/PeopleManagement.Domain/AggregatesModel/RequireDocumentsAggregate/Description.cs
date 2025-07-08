using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate
{
    public class Description : ValueObject
    {
        public const int MAX_LENGTH = 500;

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

                if (string.IsNullOrEmpty(value))
                    throw new DomainException(this, DomainErrors.FieldNotBeNullOrEmpty(nameof(Name)));

                if (value.Length > MAX_LENGTH)
                    throw new DomainException(this, DomainErrors.FieldInvalid(nameof(Name), value));


                _value = value;
            }
        }

        private Description()
        {
        }

        private Description(string value)
        {
            Value = value;
        }

        public static implicit operator Description(string value) =>
            new(value);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

    }
}
