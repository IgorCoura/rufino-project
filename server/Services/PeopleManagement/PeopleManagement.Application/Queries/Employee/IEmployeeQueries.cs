namespace PeopleManagement.Application.Queries.Employee
{
    public interface IEmployeeQueries
    {
        Task<IEnumerable<EmployeeSimpleDto>> GetEmployeeList(int pageSize, int pageNumber);
    }
}
