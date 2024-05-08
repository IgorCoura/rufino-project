namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Status : Enumeration
    {
        public static readonly Status Pending = new(1, "Pending");
        public static readonly Status Active = new(2, "Active");
        public static readonly Status Vacation = new(3, "Vacation");
        public static readonly Status Away = new(3, "Away");
        public static readonly Status Inactive = new(4, "Inactive");
        private Status(int id, string name) : base(id, name)
        {
        }
    }
}
