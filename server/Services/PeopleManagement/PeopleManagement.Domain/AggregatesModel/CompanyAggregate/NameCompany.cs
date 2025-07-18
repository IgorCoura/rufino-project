using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Text.RegularExpressions;

namespace PeopleManagement.Domain.AggregatesModel.CompanyAggregate
{
    public sealed partial class NameCompany : ValueObject
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

        private NameCompany(){
        }

        private NameCompany(string value)
        {
            Value = value;
        }

        public static implicit operator NameCompany(string value) =>
            new(value);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        [GeneratedRegex(@"^[a-zA-ZÀ-ÿ'_\\/-]+(?: [a-zA-ZÀ-ÿ'_\\/-]+)+$")]
        private static partial Regex NameRegex();
    }
}
