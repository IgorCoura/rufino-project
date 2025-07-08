using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class DirectoryName : ValueObject
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

                if (string.IsNullOrEmpty(value))
                    throw new DomainException(this, DomainErrors.FieldNotBeNullOrEmpty(nameof(FileName)));

                if (value.Length > MAX_LENGTH)
                    throw new DomainException(this, DomainErrors.FieldInvalid(nameof(FileName), value));


                _value = value;
            }
        }

        private DirectoryName()
        {
        }

        private DirectoryName(string value)
        {
            Value = value;
        }

        public static implicit operator DirectoryName(string value) =>
            new(value);

        public override string ToString() => Value;
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

    }
}
