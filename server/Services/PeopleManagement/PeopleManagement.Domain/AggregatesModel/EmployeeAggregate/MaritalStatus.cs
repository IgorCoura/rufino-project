namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class MaritalStatus : Enumeration
    {
        public static readonly MaritalStatus Single = new(1, nameof(Single));
        public static readonly MaritalStatus Married = new(2, nameof(Married));
        public static readonly MaritalStatus Divorced = new(3, nameof(Divorced));
        public static readonly MaritalStatus Widowed = new(4, nameof(Widowed));
        public static readonly MaritalStatus NotDefined = new(5, nameof(Widowed));
        private MaritalStatus(int id, string name) : base(id, name)
        {
        }

    }
}
