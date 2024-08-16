using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class ArchiveCategoryRepository(PeopleManagementContext context) : Repository<ArchiveCategory>(context), IArchiveCategoryRepository
    {
    }
}
