using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class Page : ValueObject
    {
        public const int MAX_NUMBER = 100;
        public const int MIN_NUMBER = 0;
        private int _value;

        

        public int Value
        {
            get => _value;
            private set
            {
                if (value < MIN_NUMBER || value > MAX_NUMBER)
                    throw new DomainException(this, DomainErrors.FieldMustHaveLengthBetween(nameof(Page), MIN_NUMBER, MAX_NUMBER));

                _value = value;
            }
        }

        private Page(int value)
        {
            Value = value;
        }

        public static Page Create(int value) => new(value);

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator Page(int value) => new(value);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
