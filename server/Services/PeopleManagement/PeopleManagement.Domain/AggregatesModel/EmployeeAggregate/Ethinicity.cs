namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Ethinicity : Enumeration
    {
        private static readonly Ethinicity White = new(1, nameof(White));
        private static readonly Ethinicity Black = new(2, nameof(Black));
        private static readonly Ethinicity Brown = new(3, nameof(Brown));
        private static readonly Ethinicity Yellow = new(4, nameof(Yellow));
        private static readonly Ethinicity Indigenous = new(5, nameof(Indigenous));

        private Ethinicity(int id, string name) : base(id, name)
        {
        }
    }
}
