using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentTemplateCommands;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.EditDocumentTemplate;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.InsertDocumentTemplate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;
using System.Net.Http.Headers;

namespace PeopleManagement.IntegrationTests.Tests
{
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentTemplateTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {

        // POST /documenttemplate cria o template (metadados + referências header/body/footer e grupo) e persiste igual ao esperado (ToDocumentTemplate).
        [Fact]
        public async Task CreateDocumentTemplateWithSuccess()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateDocumentTemplateCommand(
                company.Id,
                "NR01",
                "Description NR01",
                365,
                8,
                new TemplateFileInfoModel(
                    "index.html",
                    "header.html",
                    "footer.html",
                    [RecoverDataType.Employee.Id, RecoverDataType.PGR.Id]

                    ), 
                false,
                [], 
                documentGroup.Id);


            client.InputHeaders([company.Id]);
            var response = await client.PostAsJsonAsync($"/api/v1/{company.Id}/documenttemplate", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateDocumentTemplateResponse)) as CreateDocumentTemplateResponse ?? throw new ArgumentNullException();
            var result = await context.DocumentTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToDocumentTemplate(result.Id, result.TemplateFileInfo!.Directory.ToString()), result);
        }


        // POST /documenttemplate/upload envia o .zip do template: extrai os arquivos para o diretório configurado (SourceDirectory) e grava o body em disco.
        [Fact]
        public async Task InsertDocumentTemplateWithSuccess()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var documentTemplate = await context.InsertDocumentTemplate(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            using var content = new MultipartFormDataContent();

            // Carregue o arquivo PDF em um stream
            var path = Path.Combine("DataForTests", "DocumentTemplateTest.zip");
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);

            // Adicione o conteúdo do tipo arquivo ao multipart/form-data
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
            content.Add(streamContent, "formFile", Path.GetFileName(path));

            content.Add(new StringContent(documentTemplate.Id.ToString()), "id");
            content.Add(new StringContent(documentTemplate.CompanyId.ToString()), "companyId");

            client.InputHeaders([documentTemplate.CompanyId]);
            var response = await client.PostAsync($"/api/v1/{documentTemplate.CompanyId}/documenttemplate/upload", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentResponse = await response.Content.ReadFromJsonAsync(typeof(InsertDocumentTemplateResponse)) as InsertDocumentTemplateResponse ?? throw new ArgumentNullException();
            var result = await context.DocumentTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == contentResponse.Id) ?? throw new ArgumentNullException();

            using var scope = _factory.Services.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<DocumentTemplatesOptions>();

            var documentTemplatePath = Path.Combine(options.SourceDirectory, result.TemplateFileInfo!.Directory.ToString());
            Assert.True(Directory.Exists(documentTemplatePath));

            var bodyFilePath = Path.Combine(documentTemplatePath, result.TemplateFileInfo.BodyFileName.ToString());
            var bodyFile = File.OpenRead(bodyFilePath);
            Assert.NotNull(bodyFile);
            Assert.True(bodyFile.Length > 0);

            bodyFile.Dispose();

            Directory.Delete(documentTemplatePath, true);
        }


        // Round-trip EF: UsePreviousPeriod = true persiste e é relido corretamente (mapeamento da flag de competência,
        // hoje default false em toda a suíte). Garante que a flag sobrevive ao ciclo antes de virar PeriodPolicy.
        [Fact]
        public async Task CreateDocumentTemplate_WithUsePreviousPeriodTrue_RoundTrips()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, 365d, 8d,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                true, [], documentGroup.Id, usePreviousPeriod: true);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking()
                .FirstAsync(x => x.Id == template.Id, cancellationToken);

            Assert.True(persisted.UsePreviousPeriod);
        }


        // Round-trip EF das policies (Fase 2.2): a coleção owned (tabela filha + params jsonb) sobrevive ao ciclo
        // e os parâmetros voltam com o mesmo valor. Prova o mapeamento antes de os consumidores lerem por policy.
        [Fact]
        public async Task CreateDocumentTemplate_PoliciesDerivedFromFields_RoundTrip()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, 365d, 8d,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                true, [], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking()
                .FirstAsync(x => x.Id == template.Id, cancellationToken);

            Assert.Equal(2, persisted.Policies.Count);
            Assert.Equal(TimeSpan.FromDays(365), persisted.GetPolicy<IExpirationPolicy>()!.Duration);
            Assert.Equal(TimeSpan.FromHours(8), persisted.GetPolicy<IWorkloadPolicy>()!.Workload);
        }

        // Template sem vencimento/carga não materializa policy alguma: ausência da regra sobrevive ao banco
        // (presença no conjunto = regra ativa).
        [Fact]
        public async Task CreateDocumentTemplate_WithoutValidityAndWorkload_PersistsNoPolicies()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, (double?)null, null,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                true, [], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking()
                .FirstAsync(x => x.Id == template.Id, cancellationToken);

            Assert.Empty(persisted.Policies);
        }

        // Edit re-deriva as policies e o EF sincroniza a tabela filha: remover o vencimento apaga a linha da
        // ExpirationPolicy sem afetar a de carga horária.
        [Fact]
        public async Task EditDocumentTemplate_RemovingValidity_RemovesExpirationPolicyRow()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, 365d, 8d,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                true, [], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            template.Edit("NR35", "Description NR35", null, 8d,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                true, [], documentGroup.Id);
            await context.SaveChangesAsync(cancellationToken);

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking()
                .FirstAsync(x => x.Id == template.Id, cancellationToken);

            Assert.Null(persisted.GetPolicy<IExpirationPolicy>());
            Assert.Equal(TimeSpan.FromHours(8), persisted.GetPolicy<IWorkloadPolicy>()!.Workload);
        }

        // Corretude do backfill da migration AddDocumentTemplatePolicies. A migration roda contra um banco vazio
        // na suíte, então os INSERTs seriam no-op e nada provaria que o SQL reproduz o payload que o
        // DocumentPolicyFactory grava. Aqui o cenário é forçado: apaga as policies de um template já existente
        // (simulando o template legado, anterior à migration) e reaplica o mesmo SQL do backfill.
        // O SQL é uma cópia do da migration — se um mudar, este teste falha e cobra o outro.
        [Fact]
        public async Task Backfill_OnTemplateWithoutPolicies_ReproducesPoliciesFromLegacyColumns()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, 365d, 8d,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                true, [], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            await context.Database.ExecuteSqlRawAsync(
                """DELETE FROM people_management."DocumentTemplatePolicies";""", cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO people_management."DocumentTemplatePolicies" ("DocumentTemplateId", "Type", "Params")
                SELECT "Id",
                       1,
                       jsonb_build_object('DurationTicks', (EXTRACT(EPOCH FROM "DocumentValidityDuration") * 10000000)::bigint)
                FROM people_management."DocumentTemplates"
                WHERE "DocumentValidityDuration" IS NOT NULL;
                """, cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO people_management."DocumentTemplatePolicies" ("DocumentTemplateId", "Type", "Params")
                SELECT "Id",
                       4,
                       jsonb_build_object('WorkloadTicks', (EXTRACT(EPOCH FROM "Workload") * 10000000)::bigint)
                FROM people_management."DocumentTemplates"
                WHERE "Workload" IS NOT NULL;
                """, cancellationToken);

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking()
                .FirstAsync(x => x.Id == template.Id, cancellationToken);

            Assert.Equal(2, persisted.Policies.Count);
            Assert.Equal(TimeSpan.FromDays(365), persisted.GetPolicy<IExpirationPolicy>()!.Duration);
            Assert.Equal(TimeSpan.FromHours(8), persisted.GetPolicy<IWorkloadPolicy>()!.Workload);
        }

        // O backfill precisa espelhar a invariante das policies: coluna zerada é ausência de regra, não regra
        // com zero. Sem o filtro > INTERVAL '0' a migration cria linhas que o domínio recusa a reidratar
        // (DocumentPolicyFactory.ToPolicy lança), quebrando a leitura de qualquer template legado.
        [Fact]
        public async Task Backfill_OnTemplateWithZeroLegacyColumns_CreatesNoPolicies()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            // Template legado: o app manda 0, a coluna guarda 00:00:00 e nenhuma policy é derivada.
            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, 0d, 0d,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                true, [], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO people_management."DocumentTemplatePolicies" ("DocumentTemplateId", "Type", "Params")
                SELECT "Id",
                       1,
                       jsonb_build_object('DurationTicks', (EXTRACT(EPOCH FROM "DocumentValidityDuration") * 10000000)::bigint)
                FROM people_management."DocumentTemplates"
                WHERE "DocumentValidityDuration" > INTERVAL '0';
                """, cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO people_management."DocumentTemplatePolicies" ("DocumentTemplateId", "Type", "Params")
                SELECT "Id",
                       4,
                       jsonb_build_object('WorkloadTicks', (EXTRACT(EPOCH FROM "Workload") * 10000000)::bigint)
                FROM people_management."DocumentTemplates"
                WHERE "Workload" > INTERVAL '0';
                """, cancellationToken);

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking()
                .FirstAsync(x => x.Id == template.Id, cancellationToken);

            Assert.Empty(persisted.Policies);
        }

        // POST /documenttemplate com o bloco "policies" (Fase 2.4): as policies informadas mandam sobre os campos
        // escalares do payload, e os escalares são gravados como reflexo delas.
        [Fact]
        public async Task CreateDocumentTemplate_WithPoliciesBlock_PoliciesWinOverScalarFields()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateDocumentTemplateCommand(
                company.Id, "NR01", "Description NR01", 365, 8,
                new TemplateFileInfoModel("index.html", "header.html", "footer.html", [RecoverDataType.Employee.Id]),
                false, [], documentGroup.Id, false,
                new PoliciesModel(new ExpirationPolicyModel(30), new WorkloadPolicyModel(4)));

            client.InputHeaders([company.Id]);
            var response = await client.PostAsJsonAsync($"/api/v1/{company.Id}/documenttemplate", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<CreateDocumentTemplateResponse>() ?? throw new ArgumentNullException();

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking().FirstAsync(x => x.Id == content.Id, cancellationToken);

            Assert.Equal(TimeSpan.FromDays(30), persisted.GetPolicy<IExpirationPolicy>()!.Duration);
            Assert.Equal(TimeSpan.FromHours(4), persisted.GetPolicy<IWorkloadPolicy>()!.Workload);
            Assert.Equal(TimeSpan.FromDays(30), persisted.DocumentValidityDuration);
            Assert.Equal(TimeSpan.FromHours(4), persisted.Workload);
        }

        // "policies": {} = nenhuma regra ativa, mesmo com os campos escalares preenchidos no payload.
        [Fact]
        public async Task CreateDocumentTemplate_WithEmptyPoliciesBlock_PersistsNoPolicies()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateDocumentTemplateCommand(
                company.Id, "NR01", "Description NR01", 365, 8,
                new TemplateFileInfoModel("index.html", "header.html", "footer.html", [RecoverDataType.Employee.Id]),
                false, [], documentGroup.Id, false, new PoliciesModel());

            client.InputHeaders([company.Id]);
            var response = await client.PostAsJsonAsync($"/api/v1/{company.Id}/documenttemplate", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<CreateDocumentTemplateResponse>() ?? throw new ArgumentNullException();

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking().FirstAsync(x => x.Id == content.Id, cancellationToken);

            Assert.Empty(persisted.Policies);
            Assert.Null(persisted.DocumentValidityDuration);
            Assert.Null(persisted.Workload);
        }

        // Retrocompatibilidade: omitir "policies" mantém o comportamento legado (derivar dos campos escalares).
        [Fact]
        public async Task CreateDocumentTemplate_WithoutPoliciesBlock_KeepsLegacyDerivation()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateDocumentTemplateCommand(
                company.Id, "NR01", "Description NR01", 365, 8,
                new TemplateFileInfoModel("index.html", "header.html", "footer.html", [RecoverDataType.Employee.Id]),
                false, [], documentGroup.Id);

            client.InputHeaders([company.Id]);
            var response = await client.PostAsJsonAsync($"/api/v1/{company.Id}/documenttemplate", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<CreateDocumentTemplateResponse>() ?? throw new ArgumentNullException();

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking().FirstAsync(x => x.Id == content.Id, cancellationToken);

            Assert.Equal(TimeSpan.FromDays(365), persisted.GetPolicy<IExpirationPolicy>()!.Duration);
            Assert.Equal(TimeSpan.FromHours(8), persisted.GetPolicy<IWorkloadPolicy>()!.Workload);
        }

        // PUT /documenttemplate com "policies": troca o conjunto de regras e sincroniza a tabela filha.
        [Fact]
        public async Task EditDocumentTemplate_WithPoliciesBlock_ReplacesPersistedPolicies()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, 365d, 8d,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                false, [], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new EditDocumentTemplateModel(
                template.Id, "NR35", "Description NR35",
                new EditTemplateFileInfoModel("index.html", "header.html", "footer.html", [RecoverDataType.Employee.Id]),
                365, 8, false, [], documentGroup.Id, false,
                new PoliciesModel(Workload: new WorkloadPolicyModel(4)));

            client.InputHeaders([company.Id]);
            var response = await client.PutAsJsonAsync($"/api/v1/{company.Id}/documenttemplate", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking().FirstAsync(x => x.Id == template.Id, cancellationToken);

            Assert.Null(persisted.GetPolicy<IExpirationPolicy>());
            Assert.Equal(TimeSpan.FromHours(4), persisted.GetPolicy<IWorkloadPolicy>()!.Workload);
            Assert.Null(persisted.DocumentValidityDuration);
        }
    }
}
