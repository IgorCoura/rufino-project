namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events
{
    public record RequestSecurityDocumentsEvent : INotification
    {
        public Guid EmployeeId { get; private set; }
        public Guid CompanyId { get; private set; }
        public Guid RoleId { get; set; }
        private RequestSecurityDocumentsEvent(Guid employeeId, Guid companyId, Guid roleId)
        {
            EmployeeId = employeeId;
            CompanyId = companyId;
            RoleId = roleId;
        }

        public static RequestSecurityDocumentsEvent Create(Guid employeeId, Guid companyId, Guid roleId) => new(employeeId, companyId, roleId);

    }
}
