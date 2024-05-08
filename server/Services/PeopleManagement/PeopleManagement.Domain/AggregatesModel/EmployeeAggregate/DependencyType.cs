namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class DependencyType : Enumeration
    {
        public static readonly DependencyType Child = new (1, nameof(Child));
        public static readonly DependencyType Spouse = new (2, nameof(Spouse));
        private DependencyType(int id, string name) : base(id, name)
        {
        }
    }
}
