
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Text.RegularExpressions;

namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate
{
    public sealed partial class Currency : ValueObject
    {
        public const int MAX_LENGTH = 21;
        private string _value = string.Empty;
        public CurrencyType Type { get; private set; }
        public string Value 
        {
            get => _value;
            private set
            {
                value = value.Trim();

                if (!CurrencyFormat().IsMatch(value))
                    throw new DomainException(this, DomainErrors.FieldIsFormatInvalid(nameof(Value)));

                if (value.Length > MAX_LENGTH)
                    throw new DomainException(this, DomainErrors.FieldCannotBeLarger(nameof(Value), MAX_LENGTH));

                _value = value;
            }
        }

        private Currency(CurrencyType type, string value)
        {
            Type = type;
            Value = value;
        }

        public static Currency Create(CurrencyType type, string value) => new(type, value);
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
            yield return Type;
        }


        [GeneratedRegex(@"^\d+(\.\d{1,2})?$")]
        private static partial Regex CurrencyFormat();
    }
}
