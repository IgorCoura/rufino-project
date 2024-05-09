namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Disability : Enumeration
    {
        public static readonly Disability Physical = new(1, nameof(Physical));
        public static readonly Disability Intellectual = new(2, nameof(Intellectual));
        public static readonly Disability Mental = new(3, nameof(Mental));
        public static readonly Disability Auditory = new(4, nameof(Auditory));
        public static readonly Disability Visual = new(5, nameof(Visual));
        public static readonly Disability Rehabilitated = new(6, nameof(Rehabilitated));
        public static readonly Disability DisabilityQuota = new(7, nameof(DisabilityQuota));

        private Disability(int id, string name) : base(id, name)
        {
        }

        public static implicit operator Disability(int id) => Enumeration.FromValue<Disability>(id);
        public static implicit operator Disability(string name) => Enumeration.FromDisplayName<Disability>(name);
    }
}
