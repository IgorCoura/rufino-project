using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;

namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments
{
    public record CreateRequireDocumentsCommand(Guid RoleId, Guid CompanyId, string Name, string Description, params Guid[] DocumentsTemplatesIds) : IRequest<CreateRequireDocumentsResponse>
    {
        public RequireDocuments ToRequireSecurityDocuments(Guid id) => RequireDocuments.Create(id, RoleId, CompanyId, Name, Description, DocumentsTemplatesIds); 
    }

    public record CreateRequireDocumentsModel(Guid RoleId, string Name, string Description, params Guid[] DocumentsTemplatesIds)
    {
        public CreateRequireDocumentsCommand ToCommand(Guid company) => new(RoleId, company, Name, Description, DocumentsTemplatesIds);
    }
}
