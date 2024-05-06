using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    internal class ArchiveRepository : Repository<Archive>, IArchiveRepository
    {
        public ArchiveRepository(PeopleManagementContext context) : base(context)
        {
        }
    }
}
