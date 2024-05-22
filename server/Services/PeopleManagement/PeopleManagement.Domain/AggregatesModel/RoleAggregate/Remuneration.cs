namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate
{
    public class Remuneration : ValueObject
    {

        public PaymentUnit PaymentUnit { get; private set; } = null!;
        public Currency BaseSalary { get; private set; } = null!;
        public Description Description { get; private set; } = null!;

        private Remuneration() { }
        private Remuneration(PaymentUnit paymentUnit, Currency baseSalary, Description description)
        {
            PaymentUnit = paymentUnit;
            BaseSalary = baseSalary;
            Description = description;
        }

        public static Remuneration Create(PaymentUnit paymentUnit, Currency baseSalary, Description description) => new(paymentUnit, baseSalary, description);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return PaymentUnit;
            yield return BaseSalary;
            yield return Description;
        }
    }
}
