using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using System.Text.Json.Nodes;

namespace PeopleManagement.Services.Services.RecoverInfoToDocument
{
    public class RecoverComplementaryInfoToDocumentTemplateService : IRecoverComplementaryInfoToDocumentTemplateService
    {
        public Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, JsonObject[]? jsonObjects = null, CancellationToken cancellation = default)
        {
            var dateObject = DateTime.Now;
            var dateValidityObject = DateTime.Now;
            if (jsonObjects != null && jsonObjects.Length > 0)
            {
                var dateString = jsonObjects!.FirstOrDefault(x => x.ContainsKey("date"))?["date"]?.ToString();
                dateObject = DateTime.TryParse(dateString, out var parsedDate)
                    ? parsedDate
                    : DateTime.Now;
                var dateValidityString = jsonObjects!.FirstOrDefault(x => x.ContainsKey("validity"))?["validity"]?.ToString();
                dateValidityObject = DateTime.TryParse(dateValidityString, out var parsedValidityDate)
                    ? parsedValidityDate
                    : DateTime.Now;
            }
            

            var json = new JsonObject
            {
                ["ComplementaryInfo"] = new JsonObject
                {
                    ["Date"] = dateObject.ToString("dd/MM/yyyy"),
                    ["FullDatePtBr"] = dateObject.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR")),
                    ["FullValidity"] = dateValidityObject.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR")),
                    ["Validity"] = dateValidityObject.ToString("dd/MM/yyyy")
                }
            };
            return Task.FromResult(json);
        }

        public static JsonObject GetModel()
        {
            var json = new JsonObject
            {
                ["ComplementaryInfo"] = new JsonObject
                {
                    ["Date"] = DateTime.Now.ToString("dd/MM/yyyy"),
                    ["FullDatePtBr"] = DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR")),
                    ["FullValidity"] = DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR")),
                    ["Validity"] = DateTime.Now.ToString("dd/MM/yyyy")
                }
            };

            return json;
        }

    }
}
