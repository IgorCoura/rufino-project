using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class PositionRepository(PeopleManagementContext context) : Repository<Position>(context), IPositionRepository
    {
    }
}
