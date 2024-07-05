using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Text.RegularExpressions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed partial class Name : ValueObject
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

                if (!NameRegex().IsMatch(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldIsFormatInvalid(this.GetType().Name));

                if (value.Length > MAX_LENGTH)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldInvalid(this.GetType().Name, value));


                _value = value;
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


        public override string ToString() => Value;
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        [GeneratedRegex(@"^[a-zA-ZÀ-ÿ']+(?: [a-zA-ZÀ-ÿ']+)+$")]
        private static partial Regex NameRegex();
    }
}
