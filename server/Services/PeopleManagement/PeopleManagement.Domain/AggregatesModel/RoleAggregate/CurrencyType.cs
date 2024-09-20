namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate
{
    public sealed class CurrencyType : Enumeration
    {
        public static readonly CurrencyType BRL = new(1, nameof(BRL));
        public static readonly CurrencyType USD = new(2, nameof(USD));
        public static readonly CurrencyType EUR = new(3, nameof(EUR));
        private CurrencyType(int id, string name) : base(id, name)
        {
        }

        public static implicit operator CurrencyType(int id) => Enumeration.FromValue<CurrencyType>(id);
        public static implicit operator CurrencyType(string name) => Enumeration.FromDisplayName<CurrencyType>(name);

    }
}
