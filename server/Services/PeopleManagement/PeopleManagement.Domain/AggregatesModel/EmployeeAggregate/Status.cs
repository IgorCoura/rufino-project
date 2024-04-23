namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Status : Enumeration
    {
        public static Status Pending = new(1, "Pending");
        public static Status Active = new(2, "Active");
        public static Status Vacation = new(3, "Vacation");
        public static Status Away = new(3, "Away");
        public static Status Inactive = new(4, "Inactive");
        private Status(int id, string name) : base(id, name)
        {
        }
    }
}
