using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class Number : ValueObject
    {
        public const double MAX_NUMBER = 100;
        public const double MIN_NUMBER = 0;
        private double _value;

        

        public double Value
        {
            get => _value;
            private set
            {
                if (value < MIN_NUMBER || value > MAX_NUMBER)
                    throw new DomainException(this, DomainErrors.FieldMustHaveLengthBetween(nameof(Number), MIN_NUMBER, MAX_NUMBER));

                _value = value;
            }
        }

        private Number(double value)
        {
            Value = value;
        }

        public static Number Create(double value) => new(value);

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator Number(double value) => new(value);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
