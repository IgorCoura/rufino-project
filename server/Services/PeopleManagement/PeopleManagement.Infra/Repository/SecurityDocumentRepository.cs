using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class SecurityDocumentRepository(PeopleManagementContext context) : Repository<SecurityDocument>(context), ISecurityDocumentRepository
    {
    }
}
