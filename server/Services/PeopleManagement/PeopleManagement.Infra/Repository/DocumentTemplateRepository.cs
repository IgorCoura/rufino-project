using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class DocumentTemplateRepository(PeopleManagementContext context) : Repository<DocumentTemplate>(context), IDocumentTemplateRepository
    {
    }
}
