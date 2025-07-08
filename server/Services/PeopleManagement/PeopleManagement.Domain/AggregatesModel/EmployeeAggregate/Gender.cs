namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Gender : Enumeration
    {
        public static readonly Gender MALE = new(1, nameof(MALE));
        public static readonly Gender FEMALE = new(2, nameof(FEMALE));
        private Gender(int id, string name) : base(id, name)
        {
        }

        public static implicit operator Gender(int id) => Enumeration.FromValue<Gender>(id);
        public static implicit operator Gender(string name) => Enumeration.FromDisplayName<Gender>(name);
    }
}
