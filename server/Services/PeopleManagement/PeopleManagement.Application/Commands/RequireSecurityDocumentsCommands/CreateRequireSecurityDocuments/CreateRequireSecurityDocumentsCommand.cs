using PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate;

namespace PeopleManagement.Application.Commands.RequireSecurityDocumentsCommands.CreateRequireSecurityDocuments
{
    public record CreateRequireSecurityDocumentsCommand(Guid RoleId, Guid CompanyId, params Guid[] DocumentsTemplatesIds) : IRequest<CreateRequireSecurityDocumentsResponse>
    {
        public RequireSecurityDocuments ToRequireSecurityDocuments(Guid id) => RequireSecurityDocuments.Create(id, RoleId, CompanyId, DocumentsTemplatesIds); 
    }
}
