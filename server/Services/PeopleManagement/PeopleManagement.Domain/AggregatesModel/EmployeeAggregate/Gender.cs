namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Gender : Enumeration
    {
        public static readonly Gender MALE = new(1, nameof(MALE));
        public static readonly Gender FEMALE = new(2, nameof(FEMALE));
        public Gender(int id, string name) : base(id, name)
        {
        }

    }
}
