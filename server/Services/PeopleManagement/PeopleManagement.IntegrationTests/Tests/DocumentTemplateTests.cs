using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentTemplateCommands;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.EditDocumentTemplate;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.InsertDocumentTemplate;
using PeopleManagement.Application.Queries.DocumentTemplate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.ErrorTools;
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
                false, [], documentGroup.Id);
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
                false, [], documentGroup.Id);
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
                false, [], documentGroup.Id);
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

        // Migration RemoveZeroedDocumentTemplatePolicies: a primeira versão do backfill deixou linhas de policy
        // zeradas nos bancos que a aplicaram antes da correção. Depois da invariante PMD.DOCT11 essas linhas
        // param a leitura do template (GetPolicy reidrata e lança), então precisam ser removidas — quem já
        // aplicou a migration corrigida no lugar não a re-executa.
        //
        // INSERT cru aqui é intencional: o estado é inalcançável pela porta do domínio, que hoje recusa criá-lo.
        [Fact]
        public async Task CleanupMigration_OnZeroedPolicyRow_RemovesItAndUnblocksReading()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, 0d, 0d,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                false, [], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            // Recria o que o backfill antigo (WHERE ... IS NOT NULL) gravava para a coluna zerada.
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO people_management."DocumentTemplatePolicies" ("DocumentTemplateId", "Type", "Params")
                SELECT "Id", 1, jsonb_build_object('DurationTicks', 0)
                FROM people_management."DocumentTemplates"
                WHERE "DocumentValidityDuration" IS NOT NULL;
                """, cancellationToken);

            await AssertReadingTemplateThrows(template.Id, cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                DELETE FROM people_management."DocumentTemplatePolicies"
                WHERE ("Type" = 1 AND COALESCE(("Params"->>'DurationTicks')::bigint, 0) <= 0)
                   OR ("Type" = 4 AND COALESCE(("Params"->>'WorkloadTicks')::bigint, 0) <= 0);
                """, cancellationToken);

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking()
                .FirstAsync(x => x.Id == template.Id, cancellationToken);

            Assert.Empty(persisted.Policies);
            Assert.Null(persisted.GetPolicy<IExpirationPolicy>());
        }

        // Migration MoveSignatureToPolicy: o EF gerou só os DROPs (tabela PlaceSignature + coluna
        // AcceptsSignature) e avisou "may result in the loss of data" — ele não sabe para onde os dados foram.
        // Sem a migração de dados, toda configuração de assinatura sumiria. Como a suíte roda em banco novo,
        // onde não há o que migrar, o cenário legado é montado à mão.
        [Fact]
        public async Task SignatureMigration_OnLegacyRows_MovesAcceptsAndPlacesIntoThePolicy()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, (double?)null, null,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                false, [], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            // Recria o schema legado: coluna + tabela que a migration removeu, com um template que assina.
            await context.Database.ExecuteSqlRawAsync($"""
                ALTER TABLE people_management."DocumentTemplates" ADD COLUMN "AcceptsSignature" boolean NOT NULL DEFAULT false;
                CREATE TABLE people_management."PlaceSignature" (
                    "DocumentTemplateId" uuid NOT NULL,
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
                    "Page" double precision NOT NULL,
                    "RelativePositionBotton" double precision NOT NULL,
                    "RelativePositionLeft" double precision NOT NULL,
                    "RelativeSizeX" double precision NOT NULL,
                    "RelativeSizeY" double precision NOT NULL,
                    "Type" integer NOT NULL,
                    PRIMARY KEY ("DocumentTemplateId", "Id"));
                UPDATE people_management."DocumentTemplates" SET "AcceptsSignature" = true WHERE "Id" = '{template.Id}';
                INSERT INTO people_management."PlaceSignature"
                    ("DocumentTemplateId", "Type", "Page", "RelativePositionBotton", "RelativePositionLeft", "RelativeSizeX", "RelativeSizeY")
                VALUES ('{template.Id}', 2, 3, 10.5, 20.25, 30, 40);
                """, cancellationToken);

            // O mesmo SQL da migration MoveSignatureToPolicy.
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO people_management."DocumentTemplatePolicies" ("DocumentTemplateId", "Type", "Params")
                SELECT dt."Id",
                       5,
                       jsonb_build_object('PlaceSignatures', COALESCE((
                           SELECT jsonb_agg(jsonb_build_object(
                               'TypeId', ps."Type",
                               'Page', ps."Page",
                               'RelativePositionBotton', ps."RelativePositionBotton",
                               'RelativePositionLeft', ps."RelativePositionLeft",
                               'RelativeSizeX', ps."RelativeSizeX",
                               'RelativeSizeY', ps."RelativeSizeY"))
                           FROM people_management."PlaceSignature" ps
                           WHERE ps."DocumentTemplateId" = dt."Id"), '[]'::jsonb))
                FROM people_management."DocumentTemplates" dt
                WHERE dt."AcceptsSignature" = true;
                """, cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                DROP TABLE people_management."PlaceSignature";
                ALTER TABLE people_management."DocumentTemplates" DROP COLUMN "AcceptsSignature";
                """, cancellationToken);

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking()
                .FirstAsync(x => x.Id == template.Id, cancellationToken);

            Assert.True(persisted.AcceptsSignature);
            var place = Assert.Single(persisted.PlaceSignatures);
            Assert.Equal(TypeSignature.Visa, place.Type);
            Assert.Equal(3, place.Page.Value);
            Assert.Equal(10.5, place.RelativePositionBotton.Value);
            Assert.Equal(20.25, place.RelativePositionLeft.Value);
            Assert.Equal(30, place.RelativeSizeX.Value);
            Assert.Equal(40, place.RelativeSizeY.Value);
        }

        // Down da MoveSignatureToPolicy. Com o deploy derrubando coluna e tabela no mesmo passo, voltar a imagem
        // anterior não sobe — o código antigo mapeia o que deixou de existir. O rollback passa a ser rodar este
        // Down, então ele precisa funcionar: é a rede, e rede não testada não é rede.
        [Fact]
        public async Task SignatureMigrationDown_OnPolicyRow_RestoresTheLegacyColumnAndTable()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, (double?)null, null,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                true, [PlaceSignature.Create(TypeSignature.Visa, 3, 10.5, 20.25, 30, 40)], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            // A parte que o EF gera no Down: recria a estrutura.
            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE people_management."DocumentTemplates" ADD COLUMN "AcceptsSignature" boolean NOT NULL DEFAULT false;
                CREATE TABLE people_management."PlaceSignature" (
                    "DocumentTemplateId" uuid NOT NULL,
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
                    "Page" double precision NOT NULL,
                    "RelativePositionBotton" double precision NOT NULL,
                    "RelativePositionLeft" double precision NOT NULL,
                    "RelativeSizeX" double precision NOT NULL,
                    "RelativeSizeY" double precision NOT NULL,
                    "Type" integer NOT NULL,
                    PRIMARY KEY ("DocumentTemplateId", "Id"));
                """, cancellationToken);

            // A parte escrita à mão: os três SQL do Down, na ordem.
            await context.Database.ExecuteSqlRawAsync("""
                UPDATE people_management."DocumentTemplates" dt
                SET "AcceptsSignature" = true
                WHERE EXISTS (
                    SELECT 1 FROM people_management."DocumentTemplatePolicies" p
                    WHERE p."DocumentTemplateId" = dt."Id" AND p."Type" = 5);
                """, cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO people_management."PlaceSignature"
                    ("DocumentTemplateId", "Type", "Page", "RelativePositionBotton", "RelativePositionLeft", "RelativeSizeX", "RelativeSizeY")
                SELECT p."DocumentTemplateId",
                       (e->>'TypeId')::int,
                       (e->>'Page')::double precision,
                       (e->>'RelativePositionBotton')::double precision,
                       (e->>'RelativePositionLeft')::double precision,
                       (e->>'RelativeSizeX')::double precision,
                       (e->>'RelativeSizeY')::double precision
                FROM people_management."DocumentTemplatePolicies" p,
                     jsonb_array_elements(p."Params"->'PlaceSignatures') e
                WHERE p."Type" = 5;
                """, cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                DELETE FROM people_management."DocumentTemplatePolicies" WHERE "Type" = 5;
                """, cancellationToken);

            var restored = await context.Database.SqlQuery<LegacyPlaceSignatureRow>($"""
                SELECT dt."AcceptsSignature" AS "AcceptsSignature",
                       ps."Type" AS "Type",
                       ps."Page" AS "Page",
                       ps."RelativePositionBotton" AS "RelativePositionBotton",
                       ps."RelativePositionLeft" AS "RelativePositionLeft"
                FROM people_management."DocumentTemplates" dt
                JOIN people_management."PlaceSignature" ps ON ps."DocumentTemplateId" = dt."Id"
                WHERE dt."Id" = {template.Id}
                """).ToListAsync(cancellationToken);

            var policiesLeft = await context.Database.SqlQuery<int>($"""
                SELECT COUNT(*)::int AS "Value" FROM people_management."DocumentTemplatePolicies"
                WHERE "DocumentTemplateId" = {template.Id} AND "Type" = 5
                """).FirstAsync(cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                DROP TABLE people_management."PlaceSignature";
                ALTER TABLE people_management."DocumentTemplates" DROP COLUMN "AcceptsSignature";
                """, cancellationToken);

            var row = Assert.Single(restored);
            Assert.True(row.AcceptsSignature);
            Assert.Equal(TypeSignature.Visa.Id, row.Type);
            Assert.Equal(3, row.Page);
            Assert.Equal(10.5, row.RelativePositionBotton);
            Assert.Equal(20.25, row.RelativePositionLeft);
            Assert.Equal(0, policiesLeft);
        }

        // Regressão: os locais de assinatura moram na SignaturePolicy, não no TemplateFileInfo. A projeção da query
        // aninhava PlaceSignatures dentro do TemplateFileInfoDto e devolvia o bloco vazio quando o template não tinha
        // arquivo (TemplateFileInfo nulo) — então um template que aceita assinatura sem arquivo voltava do GET sem os
        // locais (some ao reabrir no app). O GetById precisa emitir os locais mesmo sem arquivo.
        [Fact]
        public async Task GetById_TemplateAcceptsSignatureWithoutFileInfo_ReturnsThePlacements()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, (double?)null, null,
                templateFileInfo: null,
                true, [PlaceSignature.Create(TypeSignature.Visa, 3, 10.5, 20.25, 30, 40)], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            using var scope = _factory.Services.CreateScope();
            var queries = scope.ServiceProvider.GetRequiredService<IDocumentTemplateQueries>();
            var dto = await queries.GetById(company.Id, template.Id);

            Assert.True(dto.AcceptsSignature);
            Assert.NotNull(dto.Policies.Signature);
            var place = Assert.Single(dto.Policies.Signature!.PlaceSignatures);
            Assert.Equal(TypeSignature.Visa.Id, place.TypeSignature.Id);
            Assert.Equal(3, place.Page);
            Assert.Equal(10.5, place.RelativePositionBotton);
            Assert.Equal(20.25, place.RelativePositionLeft);
            Assert.Equal(30, place.RelativeSizeX);
            Assert.Equal(40, place.RelativeSizeY);
        }

        // Round-trip completo do caminho do app: POST cria o template com assinatura e locais, sem arquivo (o app não
        // manda TemplateFileInfo quando o usuário só configura assinatura), e o GET devolve os locais. Guarda o fluxo
        // inteiro contra a regressão, não só a projeção.
        [Fact]
        public async Task CreateThenGetById_SignatureWithoutFileInfo_PlacementsSurviveTheRoundTrip()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateDocumentTemplateCommand(
                company.Id, "NR01", "Description NR01", (double?)null, null,
                TemplateFileInfo: null,
                AcceptsSignature: true,
                [new PlaceSignatureModel(TypeSignature.Visa.Id, 3, 10.5, 20.25, 30, 40)],
                documentGroup.Id);

            client.InputHeaders([company.Id]);
            var response = await client.PostAsJsonAsync($"/api/v1/{company.Id}/documenttemplate", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<CreateDocumentTemplateResponse>() ?? throw new ArgumentNullException();

            using var scope = _factory.Services.CreateScope();
            var queries = scope.ServiceProvider.GetRequiredService<IDocumentTemplateQueries>();
            var dto = await queries.GetById(company.Id, content.Id);

            Assert.True(dto.AcceptsSignature);
            Assert.NotNull(dto.Policies.Signature);
            var place = Assert.Single(dto.Policies.Signature!.PlaceSignatures);
            Assert.Equal(TypeSignature.Visa.Id, place.TypeSignature.Id);
            Assert.Equal(3, place.Page);
            Assert.Equal(10.5, place.RelativePositionBotton);
            Assert.Equal(20.25, place.RelativePositionLeft);
            Assert.Equal(30, place.RelativeSizeX);
            Assert.Equal(40, place.RelativeSizeY);
        }

        private sealed record LegacyPlaceSignatureRow(
            bool AcceptsSignature, int Type, double Page, double RelativePositionBotton, double RelativePositionLeft);

        private async Task AssertReadingTemplateThrows(Guid templateId, CancellationToken cancellationToken)
        {
            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var corrupted = await readContext.DocumentTemplates.AsNoTracking()
                .FirstAsync(x => x.Id == templateId, cancellationToken);

            Assert.Throws<DomainException>(() => corrupted.GetPolicy<IExpirationPolicy>());
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

        // Fase 3 (Camada 2): o bloco "period" configura a competência do template. POST persiste a PeriodPolicy
        // e o GET a devolve — é o caminho que substitui a configuração manual por SQL para os templates em conflito.
        [Fact]
        public async Task CreateDocumentTemplate_WithPeriodBlock_PersistsAndReturnsThePeriodPolicy()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateDocumentTemplateCommand(
                company.Id, "NR01", "Description NR01", (double?)null, null,
                new TemplateFileInfoModel("index.html", "header.html", "footer.html", [RecoverDataType.Employee.Id]),
                false, [], documentGroup.Id, false,
                new PoliciesModel(Period: new PeriodPolicyModel(PeriodType.Monthly.Id, UsePreviousPeriod: true)));

            client.InputHeaders([company.Id]);
            var response = await client.PostAsJsonAsync($"/api/v1/{company.Id}/documenttemplate", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<CreateDocumentTemplateResponse>() ?? throw new ArgumentNullException();

            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var persisted = await readContext.DocumentTemplates.AsNoTracking().FirstAsync(x => x.Id == content.Id, cancellationToken);

            var period = persisted.GetPolicy<IPeriodPolicy>();
            Assert.NotNull(period);
            Assert.Equal(PeriodType.Monthly, period!.PeriodType);
            Assert.True(period.UsePreviousPeriod);
            Assert.True(persisted.UsePreviousPeriod);

            var dto = await scope.ServiceProvider.GetRequiredService<IDocumentTemplateQueries>().GetById(company.Id, content.Id);
            Assert.NotNull(dto.Policies.Period);
            Assert.Equal(PeriodType.Monthly.Id, dto.Policies.Period!.PeriodTypeId);
            Assert.True(dto.Policies.Period.UsePreviousPeriod);
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

        // Reprodução do relato: adicionar o PRIMEIRO local de assinatura (lista vazia -> um) via Edit. O template
        // nasce aceitando assinatura mas sem locais; o Edit manda um local. O local precisa persistir e voltar no GET.
        [Fact]
        public async Task EditDocumentTemplate_AddFirstPlacementToEmptyList_PersistsThePlacement()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            // Template aceita assinatura, mas sem nenhum local (lista vazia) — o estado de partida do relato.
            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, (double?)null, null,
                templateFileInfo: null,
                true, [], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new EditDocumentTemplateModel(
                template.Id, "NR35", "Description NR35",
                TemplateFileInfo: null,
                null, null, true,
                [new EditPlaceSignatureModel(TypeSignature.Visa.Id, 3, 10.5, 20.25, 30, 40)],
                documentGroup.Id, false, new PoliciesModel());

            client.InputHeaders([company.Id]);
            var response = await client.PutAsJsonAsync($"/api/v1/{company.Id}/documenttemplate", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var queries = scope.ServiceProvider.GetRequiredService<IDocumentTemplateQueries>();
            var dto = await queries.GetById(company.Id, template.Id);

            Assert.True(dto.AcceptsSignature);
            Assert.NotNull(dto.Policies.Signature);
            var place = Assert.Single(dto.Policies.Signature!.PlaceSignatures);
            Assert.Equal(TypeSignature.Visa.Id, place.TypeSignature.Id);
            Assert.Equal(3, place.Page);
        }

        // Fronteira do servidor: um local sem tipo (Type = 0) é inválido — TypeSignature.FromValue(0) não existe e
        // lança ao materializar o command, então o Edit inteiro é recusado e o local não é salvo. Esse é o motivo do
        // relato "não salva ao adicionar em lista vazia": o dropdown de tipo nasce sem seleção, e sem o tipo o app
        // mandava type:0. A guarda real é o validador do cliente (o local nem é enviado); este teste apenas fixa que
        // o servidor não aceita o valor caso escape. O local anterior (que já tem tipo) salva, por isso a lista
        // não-vazia "funcionava".
        [Fact]
        public async Task EditDocumentTemplate_PlacementWithoutType_IsRejected()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var documentGroup = await context.InsertDocumentGroup(company.Id, cancellationToken);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "NR35", "Description NR35", company.Id, (double?)null, null,
                templateFileInfo: null,
                true, [], documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new EditDocumentTemplateModel(
                template.Id, "NR35", "Description NR35",
                TemplateFileInfo: null,
                null, null, true,
                [new EditPlaceSignatureModel(0, 3, 10.5, 20.25, 30, 40)],
                documentGroup.Id, false, new PoliciesModel());

            client.InputHeaders([company.Id]);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => client.PutAsJsonAsync($"/api/v1/{company.Id}/documenttemplate", command));

            // E nada foi persistido: o template segue aceitando assinatura, mas sem locais.
            using var scope = _factory.Services.CreateScope();
            var queries = scope.ServiceProvider.GetRequiredService<IDocumentTemplateQueries>();
            var dto = await queries.GetById(company.Id, template.Id);
            Assert.NotNull(dto.Policies.Signature);
            Assert.Empty(dto.Policies.Signature!.PlaceSignatures);
        }
    }
}
