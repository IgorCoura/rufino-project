using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;

namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments
{
    public record CreateRequireDocumentsCommand(Guid CompanyId, Guid AssociationId, int AssociationType, string Name, string Description, List<int> ListenEventsIds, List<Guid> DocumentsTemplatesIds) : IRequest<CreateRequireDocumentsResponse>
    {
        public RequireDocuments ToRequireSecurityDocuments(Guid id) => RequireDocuments.Create(id, CompanyId, AssociationId, AssociationType, Name, Description, ListenEventsIds, DocumentsTemplatesIds); 
    }

    public record CreateRequireDocumentsModel(Guid AssociationId, int AssociationType, string Name, string Description, List<int> ListenEventsIds, List<Guid> DocumentsTemplatesIds)
    {
        public CreateRequireDocumentsCommand ToCommand(Guid company) => new(company, AssociationId, AssociationType, Name, Description, ListenEventsIds, DocumentsTemplatesIds);
    }
}
