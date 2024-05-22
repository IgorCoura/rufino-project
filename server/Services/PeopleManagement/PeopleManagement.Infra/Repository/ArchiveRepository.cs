using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class ArchiveRepository(PeopleManagementContext context) : Repository<Archive>(context), IArchiveRepository
    {
    }
}
