using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class RoleRepository(PeopleManagementContext context) : Repository<Role>(context), IRoleRepository
    {
    }
}
