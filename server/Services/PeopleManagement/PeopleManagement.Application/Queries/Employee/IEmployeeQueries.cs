namespace PeopleManagement.Application.Queries.Employee
{
    public interface IEmployeeQueries
    {
        Task<IEnumerable<EmployeeSimpleDto>> GetEmployeeList(EmployeeParams pms, Guid company);
    }
}
