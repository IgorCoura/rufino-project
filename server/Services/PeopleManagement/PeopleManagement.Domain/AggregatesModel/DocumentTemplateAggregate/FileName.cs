using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class FileName : ValueObject
    {
        public const int MAX_LENGTH = 20;

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

                var extesion = Path.GetExtension(value).ToLower().Trim();

                if (extesion != ".html")
                    throw new DomainException(this, DomainErrors.FieldIsFormatInvalid(nameof(FileName)));

                _value = value;
            }
        }

        private FileName()
        {
        }

        private FileName(string value)
        {
            Value = value;
        }

        public static implicit operator FileName(string value) =>
            new(value);

        public override string ToString() => Value;
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

    }
}
