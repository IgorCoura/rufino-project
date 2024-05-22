using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate
{
    public class CBO : ValueObject
    {
        public const int MAX_LENGTH = 6;

        private string _value = string.Empty;

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                var temp = value.Select(x => char.IsDigit(x) ? x : ' ').ToArray();
                value = new string(temp).Replace(" ", "");

                if (string.IsNullOrEmpty(value))
                    throw new DomainException(this, DomainErrors.FieldNotBeNullOrEmpty(nameof(Name)));

                if (value.Length > MAX_LENGTH)
                    throw new DomainException(this, DomainErrors.FieldInvalid(nameof(Name), value));


                _value = value;
            }
        }

        private CBO()
        {
        }

        private CBO(string value)
        {
            Value = value;
        }

        public static implicit operator CBO(string value) =>
            new(value);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

    }
}
