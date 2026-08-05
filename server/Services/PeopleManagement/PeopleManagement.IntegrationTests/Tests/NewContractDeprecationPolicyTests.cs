using System.Net;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.EmployeeCommands.CompleteAdmissionEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.FinishedContractEmployee;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using Document = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Document;

namespace PeopleManagement.IntegrationTests.Tests
{
    /// <summary>
    /// NewContractDeprecationPolicy de ponta a ponta: o funcionário é desligado e readmitido pelos endpoints
    /// reais, e a conclusão da admissão (o único caminho que abre um contrato de trabalho) deprecia as unidades
    /// já entregues — mas só dos documentos cujo template compõe a regra. O template sem a regra é o
    /// contraponto: seus documentos atravessam o novo contrato intactos.
    ///
    /// A policy é lida AO VIVO do template, então o mesmo par de templates prova as duas metades numa
    /// readmissão só.
    /// </summary>
    [Collection(nameof(IntegrationTestCollection))]
    public class NewContractDeprecationPolicyTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        [Fact]
        public async Task CompleteAdmission_WithNewContractDeprecationPolicy_ShouldDeprecateOnlyItsDeliveredUnits()
        {
            var ct = CancellationToken.None;
            var seed = await SeedReadmittableEmployeeAsync(ct);

            await FinishContractAsync(seed, ct);
            await CompleteAdmissionAsync(seed, ct);

            var withPolicy = await GetDocumentAsync(seed.DocumentWithPolicyId, ct);
            Assert.Equal(DocumentUnitStatus.Deprecated,
                withPolicy.DocumentsUnits.First(x => x.Id == seed.DeliveredUnitWithPolicyId).Status);

            var withoutPolicy = await GetDocumentAsync(seed.DocumentWithoutPolicyId, ct);
            Assert.Equal(DocumentUnitStatus.OK,
                withoutPolicy.DocumentsUnits.First(x => x.Id == seed.DeliveredUnitWithoutPolicyId).Status);
        }

        // A depreciação roda ANTES da geração das unidades do evento de admissão: o documento fica cobrável de
        // novo (pendente esperando entrega) em vez de ficar sem unidade nenhuma.
        [Fact]
        public async Task CompleteAdmission_WithNewContractDeprecationPolicy_ShouldLeaveAPendingUnitForTheNewContract()
        {
            var ct = CancellationToken.None;
            var seed = await SeedReadmittableEmployeeAsync(ct);

            await FinishContractAsync(seed, ct);
            await CompleteAdmissionAsync(seed, ct);

            var withPolicy = await GetDocumentAsync(seed.DocumentWithPolicyId, ct);

            Assert.Contains(withPolicy.DocumentsUnits, x => x.Status == DocumentUnitStatus.Pending);
        }

        // Unidade ainda em curso não é entrega de contrato nenhum — a readmissão não pode invalidá-la.
        [Fact]
        public async Task CompleteAdmission_WithPendingUnit_ShouldLeaveItPending()
        {
            var ct = CancellationToken.None;
            var seed = await SeedReadmittableEmployeeAsync(ct);

            await FinishContractAsync(seed, ct);
            await CompleteAdmissionAsync(seed, ct);

            var withPolicy = await GetDocumentAsync(seed.DocumentWithPolicyId, ct);

            Assert.Equal(DocumentUnitStatus.Pending,
                withPolicy.DocumentsUnits.First(x => x.Id == seed.PendingUnitWithPolicyId).Status);
        }

        private sealed record ReadmissionSeed(
            Guid CompanyId,
            Guid EmployeeId,
            Guid DocumentWithPolicyId,
            Guid DeliveredUnitWithPolicyId,
            Guid PendingUnitWithPolicyId,
            Guid DocumentWithoutPolicyId,
            Guid DeliveredUnitWithoutPolicyId);

        /// <summary>
        /// Funcionário ativo (já admitido uma vez) com dois documentos entregues: um de template COM a regra,
        /// outro de template SEM ela. O de template com a regra também carrega uma unidade pendente, para provar
        /// que o que está em curso sobrevive.
        /// </summary>
        private async Task<ReadmissionSeed> SeedReadmittableEmployeeAsync(CancellationToken ct)
        {
            var context = GetContext();

            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var documentGroup = await context.InsertDocumentGroup(company.Id, ct);

            var templateWithPolicy = CreateTemplate(company.Id, documentGroup.Id, "Com Depreciação",
                [new NewContractDeprecationPolicy()]);
            var templateWithoutPolicy = CreateTemplate(company.Id, documentGroup.Id, "Sem Depreciação", []);

            await context.DocumentTemplates.AddRangeAsync([templateWithPolicy, templateWithoutPolicy], ct);
            await context.SaveChangesAsync(ct);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [role.Id],
                AssociationType.Role, "Docs da admissão", "Documentos exigidos na admissão",
                [ListenEvent.Create(EmployeeEvent.COMPLETE_ADMISSION_EVENT, [Status.Active.Id])],
                [templateWithPolicy.Id, templateWithoutPolicy.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);
            await context.SaveChangesAsync(ct);

            var (documentWithPolicy, deliveredWithPolicy) =
                await InsertDeliveredDocumentAsync(context, employee, requireDocuments.Id, templateWithPolicy, ct);
            var pendingWithPolicy = documentWithPolicy.NewDocumentUnit(Guid.NewGuid());

            var (documentWithoutPolicy, deliveredWithoutPolicy) =
                await InsertDeliveredDocumentAsync(context, employee, requireDocuments.Id, templateWithoutPolicy, ct);

            await context.SaveChangesAsync(ct);

            return new ReadmissionSeed(company.Id, employee.Id,
                documentWithPolicy.Id, deliveredWithPolicy, pendingWithPolicy.Id,
                documentWithoutPolicy.Id, deliveredWithoutPolicy);
        }

        // Sem arquivo e sem validade: o foco é o estado da unidade, e uma validade gravada agendaria o job de
        // vencimento, que não tem nada a ver com o que este teste mede.
        private static DocumentTemplate CreateTemplate(Guid companyId, Guid documentGroupId, string name,
            IEnumerable<IDocumentPolicy> policies)
            => DocumentTemplate.Create(
                Guid.NewGuid(), name, $"Descrição {name}", companyId,
                (double?)null, null,
                templateFileInfo: null,
                acceptsSignature: false,
                placeSignatures: [],
                documentGroupId: documentGroupId,
                usePreviousPeriod: false,
                policies: policies);

        private static async Task<(Document document, Guid deliveredUnitId)> InsertDeliveredDocumentAsync(
            PeopleManagementContext context, Employee employee, Guid requireDocumentsId, DocumentTemplate template,
            CancellationToken ct)
        {
            var document = Document.Create(Guid.NewGuid(), employee.Id, employee.CompanyId, requireDocumentsId,
                template.Id, template.Name.ToString(), template.Description.ToString());

            var unit = document.NewDocumentUnit(Guid.NewGuid());
            document.UpdateDocumentUnitDetails(unit.Id, DateOnly.FromDateTime(DateTime.UtcNow), TimeSpan.Zero, "");
            document.InsertUnitWithoutRequireValidation(unit.Id, "file", "pdf");

            await context.Documents.AddAsync(document, ct);

            return (document, unit.Id);
        }

        private async Task FinishContractAsync(ReadmissionSeed seed, CancellationToken ct)
        {
            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);

            var command = new FinishedContractEmployeeModel(seed.EmployeeId, DateOnly.FromDateTime(DateTime.UtcNow));
            var response = await client.PutAsJsonAsync($"/api/v1/{seed.CompanyId}/employee/contract/finished", command, ct);

            var body = await response.Content.ReadAsStringAsync(ct);
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");
        }

        private async Task CompleteAdmissionAsync(ReadmissionSeed seed, CancellationToken ct)
        {
            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);

            var command = new CompleteAdmissionEmployeeModel(seed.EmployeeId, "RU2026",
                DateOnly.FromDateTime(DateTime.UtcNow), EmploymentContractType.CLT.Id);
            var response = await client.PutAsJsonAsync($"/api/v1/{seed.CompanyId}/employee/admission/complete", command, ct);

            var body = await response.Content.ReadAsStringAsync(ct);
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");
        }
    }
}
