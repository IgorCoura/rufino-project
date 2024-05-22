namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate
{
    public sealed class CurrencyType : Enumeration
    {
        public static readonly CurrencyType Real = new(1, nameof(Real));
        public static readonly CurrencyType Dolar = new(2, nameof(Dolar));
        public static readonly CurrencyType Euro = new(3, nameof(Euro));
        private CurrencyType(int id, string name) : base(id, name)
        {
        }

        public static implicit operator CurrencyType(int id) => Enumeration.FromValue<CurrencyType>(id);
        public static implicit operator CurrencyType(string name) => Enumeration.FromDisplayName<CurrencyType>(name);

    }
}
