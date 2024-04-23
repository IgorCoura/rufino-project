namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Kinship : Enumeration
    {
        public static readonly Kinship Child = new (1, nameof(Child));
        public static readonly Kinship Spouse = new (2, nameof(Spouse));
        private Kinship(int id, string name) : base(id, name)
        {
        }
    }
}
