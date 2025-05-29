using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using System.Text.Json.Nodes;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate.Interfaces;
using System.Text.Json;

namespace PeopleManagement.Services.Services.RecoverInfoToDocument
{
    public class RecoverRoleInfoToDocumentTemplateService(IRoleRepository roleRepository) : IRecoverRoleInfoToDocumentTemplateService
    {
        private readonly IRoleRepository _roleRepository = roleRepository;
        public async Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, CancellationToken cancellation = default)
        {
            var role = await _roleRepository.GetRoleFromEmployeeId(employeeId, companyId, cancellation)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Role), employeeId.ToString()!));

            var roleJson = new JsonObject
            {
                ["Id"] = role.Id.ToString(),
                ["Name"] = role.Name.ToString(),
                ["Description"] = role.Description.ToString(),
                ["CBO"] = role.CBO.ToString(),
                ["Remuneration"] = JsonSerializer.Serialize(role.Remuneration)
            };

            return roleJson;
        }
    }
}
