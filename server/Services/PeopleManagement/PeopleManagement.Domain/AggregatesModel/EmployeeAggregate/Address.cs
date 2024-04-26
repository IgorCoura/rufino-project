using System.Text.RegularExpressions;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public partial class Address : ValueObject
    {
        public const int MAX_LENGHT_STREET = 100;
        public const int MAX_LENGHT_NUMBER = 10;
        public const int MAX_LENGHT_COMPLEMENT = 50;
        public const int MAX_LENGHT_NEIGHBORHOOD = 50;
        public const int MAX_LENGHT_CITY = 50;
        public const int MAX_LENGHT_STATE = 50;
        public const int MAX_LENGHT_COUNTRY = 50;

        private string _zipCode = string.Empty;
        private string _street = string.Empty;
        private string _number = string.Empty;
        private string _complement = string.Empty;
        private string _neighborhood = string.Empty;
        private string _city = string.Empty;
        private string _state = string.Empty;
        private string _country = string.Empty;
        public string ZipCode 
        {
            get => _zipCode;
            private set
            {
                value = value.ToUpper();
                value = value.ToLower().Trim().Replace("-", "");

                if (!ZipCodeRegex().IsMatch(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldIsFormatInvalid(nameof(ZipCode)));

                _zipCode = value;
            }
        }
        public string Street 
        {
            get => _street;
            private set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Street)));

                if (value.Length > MAX_LENGHT_STREET)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Street), MAX_LENGHT_STREET));

                _street = value;
            }
        } 
        public string Number 
        {
            get => _number;
            private set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Number)));

                if (value.Length > MAX_LENGHT_NUMBER)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Number), MAX_LENGHT_NUMBER));

                _number = value;
            }
        }
        public string Complement 
        {
            get => _complement;
            private set
            {
                value = value.ToUpper().Trim();

                if (value.Length > MAX_LENGHT_COMPLEMENT)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Complement), MAX_LENGHT_COMPLEMENT));

                _complement = value;
            }
        }
        public string Neighborhood 
        {
            get => _neighborhood;
            private set
            {
                value = value.ToUpper().Trim();
                 
                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Neighborhood)));

                if (value.Length > MAX_LENGHT_NEIGHBORHOOD)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Neighborhood), MAX_LENGHT_NEIGHBORHOOD));

                _neighborhood = value;
            }
            
        }
        public string City 
        {
            get => _city;
            private set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(City)));

                if (value.Length > MAX_LENGHT_CITY)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(City), MAX_LENGHT_CITY));

                _city = value;
            }
        }
        public string State 
        {
            get => _state;
            private set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(State)));

                if (value.Length > MAX_LENGHT_STATE)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(State), MAX_LENGHT_STATE));

                _state = value;
            }
        } 
        public string Country 
        {
            get => _country;
            private set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Country)));

                if (value.Length > MAX_LENGHT_COUNTRY)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Country), MAX_LENGHT_COUNTRY));

                _country = value;
            }
        }

        private Address(string zipCode, string street, string number, string complement, string neighborhood, string city, string state, string country)
        {
            ZipCode = zipCode;
            Street = street;
            Number = number;
            Complement = complement;
            Neighborhood = neighborhood;
            City = city;
            State = state;  
            Country = country;
        }

        public static Address Create(string zipCode, string street, string number, string complement, string neighborhood, string city, string state, string country)
        {
            return new Address(zipCode, street, number, complement, neighborhood, city, state, country);
        }


        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Number;
            yield return Complement;
            yield return Neighborhood;
            yield return Street;
            yield return City;
            yield return State;
            yield return Country;
            yield return ZipCode;
        }

        [GeneratedRegex(@"^\d{8}")]
        private static partial Regex ZipCodeRegex();
     
    }
}
