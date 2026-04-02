using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;

namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.EditRequireSecurityDocuments
{
    public record EditRequireDocumentsCommand(Guid Id, Guid CompanyId, List<Guid> AssociationIds, int AssociationType, string Name, string Description, List<EditListenEventModel> ListenEvents, List<Guid> DocumentsTemplatesIds) : IRequest<EditRequireDocumentsResponse>
    {
        public RequireDocuments ToRequireDocuments() => RequireDocuments.Create(Id, CompanyId, AssociationIds, AssociationType, Name, Description, ListenEvents.Select(x => ListenEvent.Create(x.EventId, x.Status.ToList())).ToList(), DocumentsTemplatesIds);
    }

    public record EditRequireDocumentsModel(Guid Id, List<Guid> AssociationIds, int AssociationType, string Name, string Description, List<EditListenEventModel> ListenEvents, List<Guid> DocumentsTemplatesIds)
    {
        public EditRequireDocumentsCommand ToCommand(Guid company) => new(Id, company, AssociationIds, AssociationType, Name, Description, ListenEvents, DocumentsTemplatesIds);
    }

    public record EditListenEventModel(int EventId, int[] Status)
    {
        public ListenEvent ToObjectValue() => ListenEvent.Create(EventId, Status.ToList());
    }
}
