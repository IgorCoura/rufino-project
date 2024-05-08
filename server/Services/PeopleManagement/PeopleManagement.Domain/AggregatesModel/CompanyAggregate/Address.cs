namespace PeopleManagement.Domain.AggregatesModel.CompanyAggregate
{
    public sealed class Address : ValueObject
    {
        public string ZipCode { get; } = string.Empty;
        public string Street { get;  } = string.Empty;
        public string Number { get; } = string.Empty;
        public string Complement { get; } = string.Empty;
        public string Neighborhood { get; } = string.Empty;
        public string City { get; } = string.Empty;
        public string State { get; } = string.Empty;
        public string Country { get; } = string.Empty;

        private Address() { }

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

        public static Address Default()
        {
            return new Address();
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
    }
}
