namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate
{
    public class PaymentUnit : Enumeration
    {
        public static readonly PaymentUnit NotApplicable = new(0, nameof(NotApplicable));
        public static readonly PaymentUnit PerHour = new(1, nameof(PerHour));
        public static readonly PaymentUnit PerDay = new(2, nameof(PerDay));
        public static readonly PaymentUnit PerWeek = new(3, nameof(PerWeek));
        public static readonly PaymentUnit PerFortnight = new(4, nameof(PerFortnight));
        public static readonly PaymentUnit PerMonth = new(5, nameof(PerMonth));
        public static readonly PaymentUnit PerTask = new (6, nameof(PerTask));
        
        private PaymentUnit(int id, string name) : base(id, name)
        {
        }

        public static implicit operator PaymentUnit(int id) => Enumeration.FromValue<PaymentUnit>(id);
        public static implicit operator PaymentUnit(string name) => Enumeration.FromDisplayName<PaymentUnit>(name);
    }
}
