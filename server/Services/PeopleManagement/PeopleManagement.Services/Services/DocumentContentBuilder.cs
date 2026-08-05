using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.Utils;
using System.Text.Json.Nodes;

namespace PeopleManagement.Services.Services
{
    public class DocumentContentBuilder(IServiceProvider serviceProvider, ILogger<DocumentContentBuilder> logger) : IDocumentContentBuilder
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<DocumentContentBuilder> _logger = logger;

        // Memo por (funcionário, tipo de dado) válido enquanto o builder viver — ele é scoped, então o escopo é a
        // request. Dado de funcionário não muda no meio de uma request, e os lotes (batch update, verificação de
        // desatualização) repetem o mesmo par dezenas de vezes. As fontes nunca são mutadas pelo merge (ele copia
        // tudo para um objeto novo), então devolver a mesma instância é seguro.
        private readonly Dictionary<(Guid EmployeeId, int RecoverDataTypeId), JsonObject> _recoveredCache = [];

        public async Task<DocumentContentResult> Build(IEnumerable<RecoverDataType> recoverDataTypes, Guid employeeId, Guid companyId,
            DateOnly date, DateOnly? validity, DateOnly? workloadEndDate, CancellationToken cancellationToken = default)
        {
            var objects = new List<JsonObject>();
            var failedTypes = new List<RecoverDataType>();

            // O bloco complementar depende dos valores da unidade, então nunca é cacheado — e, ao contrário dos
            // demais, uma falha aqui propaga: sem ele o conteúdo não tem data nenhuma.
            var complementaryService = GetServiceToRecoverData(RecoverDataType.ComplementaryInfo, _serviceProvider);
            var complementaryInfo = await complementaryService.RecoverInfo(employeeId, companyId,
                jsonObjects: [
                    new JsonObject{
                        ["date"] = $"{date}",
                        ["validity"] = $"{validity}",
                        ["workloadEndDate"] = $"{workloadEndDate}"
                    },
                    ],
                cancellation: cancellationToken);
            objects.Add(complementaryInfo);

            foreach (var recoverDataType in recoverDataTypes)
            {
                if (recoverDataType == RecoverDataType.ComplementaryInfo)
                    continue;

                var cacheKey = (employeeId, recoverDataType.Id);
                if (_recoveredCache.TryGetValue(cacheKey, out var cached))
                {
                    objects.Add(cached);
                    continue;
                }

                try
                {
                    var recoverDataService = GetServiceToRecoverData(recoverDataType, _serviceProvider);
                    var jsonObject = await recoverDataService.RecoverInfo(employeeId, companyId, cancellation: cancellationToken);
                    _recoveredCache[cacheKey] = jsonObject;
                    objects.Add(jsonObject);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to recover data of type {RecoverDataType} for employee {EmployeeId} in company {CompanyId}. Skipping this data type.",
                        recoverDataType.Name, employeeId, companyId);
                    failedTypes.Add(recoverDataType);
                    continue;
                }
            }

            var result = objects.MergeListJsonObjects();
            return new DocumentContentResult(result.ToString(), failedTypes);
        }

        private static IRecoverInfoToDocumentTemplateService GetServiceToRecoverData(RecoverDataType doc, IServiceProvider provider)
        {
            var result = provider.GetRequiredService(doc.Type) as IRecoverInfoToDocumentTemplateService
                ?? throw new NullReferenceException($"O Serviço de tipo {doc.Type} não foi injetado.");
            return result;
        }
    }
}
