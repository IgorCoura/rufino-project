using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;

namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments
{
    public record CreateRequireDocumentsCommand(Guid CompanyId, Guid AssociationId, int AssociationType, string Name, string Description, List<ListenEventModel> ListenEvents, List<Guid> DocumentsTemplatesIds) : IRequest<CreateRequireDocumentsResponse>
    {
        public RequireDocuments ToRequireDocuments(Guid id) => RequireDocuments.Create(id, CompanyId, AssociationId, AssociationType, Name, Description, ListenEvents.Select(x => ListenEvent.Create(x.EventId, x.Status.ToList())).ToList(), DocumentsTemplatesIds); 
    }

    public record CreateRequireDocumentsModel(Guid AssociationId, int AssociationType, string Name, string Description, List<ListenEventModel> ListenEvents, List<Guid> DocumentsTemplatesIds)
    {
        public CreateRequireDocumentsCommand ToCommand(Guid company) => new(company, AssociationId, AssociationType, Name, Description, ListenEvents, DocumentsTemplatesIds);
    }

    public record ListenEventModel(int EventId, int[] Status)
    {

    }
}
