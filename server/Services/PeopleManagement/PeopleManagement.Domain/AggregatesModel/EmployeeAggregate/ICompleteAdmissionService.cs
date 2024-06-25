namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public interface ICompleteAdmissionService
    {
        Task<Employee> CompleteAdmission(Guid employeeId, Guid companyId, Registration registration, DateOnly dateInit, EmploymentContactType contractType, CancellationToken cancellationToken = default);
    }
}
