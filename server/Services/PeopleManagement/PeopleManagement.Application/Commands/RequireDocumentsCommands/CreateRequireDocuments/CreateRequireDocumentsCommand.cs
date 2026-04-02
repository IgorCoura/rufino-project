using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;

namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments
{
    public record CreateRequireDocumentsCommand(Guid CompanyId, List<Guid> AssociationIds, int AssociationType, string Name, string Description, List<ListenEventModel> ListenEvents, List<Guid> DocumentsTemplatesIds) : IRequest<CreateRequireDocumentsResponse>
    {
        public RequireDocuments ToRequireDocuments(Guid id) => RequireDocuments.Create(id, CompanyId, AssociationIds, AssociationType, Name, Description, ListenEvents.Select(x => ListenEvent.Create(x.EventId, x.Status.ToList())).ToList(), DocumentsTemplatesIds);
    }

    public record CreateRequireDocumentsModel(List<Guid> AssociationIds, int AssociationType, string Name, string Description, List<ListenEventModel> ListenEvents, List<Guid> DocumentsTemplatesIds)
    {
        public CreateRequireDocumentsCommand ToCommand(Guid company) => new(company, AssociationIds, AssociationType, Name, Description, ListenEvents, DocumentsTemplatesIds);
    }

    public record ListenEventModel(int EventId, int[] Status)
    {

    }
}
