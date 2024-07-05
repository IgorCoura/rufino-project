using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class DepartamentRepository(PeopleManagementContext context) : Repository<Department>(context), IDepartmentRepository
    {
    }
}
