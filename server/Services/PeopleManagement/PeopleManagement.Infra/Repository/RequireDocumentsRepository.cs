using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class RequireDocumentsRepository(PeopleManagementContext context) : Repository<RequireDocuments>(context), IRequireDocumentsRepository
    {
    }
}
