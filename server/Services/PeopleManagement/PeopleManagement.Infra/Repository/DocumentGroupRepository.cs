using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class DocumentGroupRepository(PeopleManagementContext context) :  Repository<DocumentGroup>(context), IDocumentGroupRepository
    {
    }
}
