namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events
{
    public record RequestDocumentsEvent : INotification
    {
        public Guid EmployeeId { get; private set; }
        public Guid CompanyId { get; private set; }
        public Guid AssociationId { get; set; }
        private RequestDocumentsEvent(Guid employeeId, Guid companyId, Guid associationId)
        {
            EmployeeId = employeeId;
            CompanyId = companyId;
            AssociationId = associationId;
        }

        public static RequestDocumentsEvent Create(Guid employeeId, Guid companyId, Guid associationId) => new(employeeId, companyId, associationId);

    }
}
