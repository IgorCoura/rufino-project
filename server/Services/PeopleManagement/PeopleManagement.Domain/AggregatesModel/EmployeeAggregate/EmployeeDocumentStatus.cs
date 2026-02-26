using PeopleManagement.Domain.SeedWord;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class EmployeeDocumentStatus : Enumeration
    {
        public static readonly EmployeeDocumentStatus Okay = new(0, nameof(Okay));
        public static readonly EmployeeDocumentStatus Warning = new(1, nameof(Warning));
        public static readonly EmployeeDocumentStatus RequiresAttention = new(2, nameof(RequiresAttention));

        private EmployeeDocumentStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator EmployeeDocumentStatus(int id) => Enumeration.FromValue<EmployeeDocumentStatus>(id);
        public static implicit operator EmployeeDocumentStatus(string name) => Enumeration.FromDisplayName<EmployeeDocumentStatus>(name);
    }
}
