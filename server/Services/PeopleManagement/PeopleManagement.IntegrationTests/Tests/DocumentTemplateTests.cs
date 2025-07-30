using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.InsertDocumentTemplate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PeopleManagement.IntegrationTests.Tests
{
    public class DocumentTemplateTests(PeopleManagementWebApplicationFactory factory) : IClassFixture<PeopleManagementWebApplicationFactory>
    {
        private readonly PeopleManagementWebApplicationFactory _factory = factory;

        [Fact]
        public async Task CreateDocumentTemplateWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

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
                    
                    ), [], documentGroup.Id);


            client.InputHeaders([company.Id]);
            var response = await client.PostAsJsonAsync($"/api/v1/{company.Id}/documenttemplate", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateDocumentTemplateResponse)) as CreateDocumentTemplateResponse ?? throw new ArgumentNullException();
            var result = await context.DocumentTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToDocumentTemplate(result.Id, result.TemplateFileInfo!.Directory.ToString()), result);
        }


        [Fact]
        public async Task InsertDocumentTemplateWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

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
            var cont = await response.Content.ReadAsStringAsync();
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


    }
}
