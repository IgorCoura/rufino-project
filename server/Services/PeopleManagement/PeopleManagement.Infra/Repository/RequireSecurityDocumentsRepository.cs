using PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class RequireSecurityDocumentsRepository(PeopleManagementContext context) : Repository<RequireSecurityDocuments>(context), IRequireSecurityDocumentsRepository
    {
    }
}
