using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.InsertDocumentTemplate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
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


    }
}
