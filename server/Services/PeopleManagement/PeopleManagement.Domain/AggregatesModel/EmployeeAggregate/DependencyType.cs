namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class DependencyType : Enumeration
    {
        public static readonly DependencyType Child = new (1, nameof(Child));
        public static readonly DependencyType Spouse = new (2, nameof(Spouse));
        private DependencyType(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DependencyType(int id) => Enumeration.FromValue<DependencyType>(id);
        public static implicit operator DependencyType(string name) => Enumeration.FromDisplayName<DependencyType>(name);
    }
}
