using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;

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
                ["Role"] = new JsonObject
                {
                    ["Id"] = role.Id.ToString(),
                    ["Name"] = role.Name.ToString(),
                    ["Description"] = role.Description.ToString(),
                    ["CBO"] = role.CBO.ToString(),
                    ["Remuneration"] = ConvertRemunerationToJsonObject(role.Remuneration)
                }
            };

            return roleJson;
        }

        public static JsonObject GetModel()
        {
            var remuneration = Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "10.55"), "Por Hora");

            var json = new JsonObject
            {
                ["Role"] = new JsonObject
                {
                    ["Id"] = Guid.Empty.ToString(),
                    ["Name"] = "role.Name",
                    ["Description"] = "role.Description",
                    ["CBO"] = "role.CBO",
                    ["Remuneration"] = ConvertRemunerationToJsonObject(remuneration)
                }
            };


            return json;
        }

        private static JsonObject ConvertRemunerationToJsonObject(Remuneration remuneration)
        {
            return new JsonObject
            {
                ["PaymentUnit"] = remuneration.PaymentUnit.ToString(),
                ["BaseSalary"] = new JsonObject
                {
                    ["Type"] = remuneration.BaseSalary.Type.ToString(),
                    ["Value"] = remuneration.BaseSalary.Value
                },
                ["Description"] = remuneration.Description.Value
            };
        }
    }
}
