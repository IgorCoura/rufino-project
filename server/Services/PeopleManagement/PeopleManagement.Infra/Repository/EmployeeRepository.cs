using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class EmployeeRepository(PeopleManagementContext context) : Repository<Employee>(context), IEmployeeRepository
    {

    }
}
