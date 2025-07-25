﻿using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Registration : ValueObject
    {
        public const int MAX_LENGTH = 15;
        private string _value = string.Empty;       
        public string Value
        {
            get => _value;
            private set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Registration)));

                if (value.Length > MAX_LENGTH)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Registration), MAX_LENGTH));

                _value = value;
            }
        }

        private Registration(string value)
        {
            Value = value;
        }

        public static Registration Create(string value) => new(value);


        public static implicit operator Registration(string input) =>
            new(input);

        public override string ToString() => Value;
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
