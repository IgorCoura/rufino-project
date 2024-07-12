using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.SecurityDocumentCommands.CreateDocument;
using PeopleManagement.Application.Commands.SecurityDocumentCommands.InsertDocument;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Options;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;
using System.Net.Http.Headers;

namespace PeopleManagement.IntegrationTests.Tests
{
    public class SecurityDocumentTests(PeopleManagementWebApplicationFactory factory) : IClassFixture<PeopleManagementWebApplicationFactory>
    {
        private readonly PeopleManagementWebApplicationFactory _factory = factory;
        [Fact]
        public async Task CreateDocumentWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var securityDocument = await context.InsertSecurityDocument(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var date = DateTime.UtcNow;
            var command = new CreateDocumentCommand(
                    securityDocument.Id,
                    securityDocument.EmployeeId,
                    securityDocument.CompanyId,
                    date
                );
 

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PostAsJsonAsync("/api/v1/securitydocument/create", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateDocumentResponse)) as CreateDocumentResponse ?? throw new ArgumentNullException();
            var result = await context.SecurityDocuments.AsNoTracking().Include(x => x.Documents.Where(x => x.Id == content.Id)).FirstOrDefaultAsync(x => x.Id == securityDocument.Id) ?? throw new ArgumentNullException();
            var document = result.Documents.First();
            Assert.Equal(date.Minute, document.Date.Minute);
        }

        [Fact]
        public async Task GeneratePdfWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();
            
            var securityDocument = await context.InsertSecurityDocument(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            var document = await securityDocument.InsertOneDocumentInSecurityDocument(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.GetAsync($"/api/v1/securitydocument/pdf/{document.Id}/{securityDocument.Id}/{securityDocument.EmployeeId}/{securityDocument.CompanyId}");         

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsByteArrayAsync();
            Assert.NotNull(content);
            Assert.True(content.Length > 149000 && content.Length < 153000);

            //Debug
            //var directory = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            //var path = Path.Combine(directory, $"{Guid.NewGuid()}.pdf");
            //if(!Directory.Exists(directory))
            //    Directory.CreateDirectory(directory);
            //File.WriteAllBytes(path, content);
        }

        [Fact]
        public async Task InsertPdfWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var securityDocument = await context.InsertSecurityDocument(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            var document = await securityDocument.InsertOneDocumentInSecurityDocument(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);


            using var content = new MultipartFormDataContent();

            // Carregue o arquivo PDF em um stream
            var path = Path.Combine("DataForTests", "199f760b-601d-4a05-aee4-d0a9dbcc6b4d.pdf");
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);
                
            // Adicione o conteúdo do tipo arquivo ao multipart/form-data
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(streamContent, "formFile", Path.GetFileName(path));

            content.Add(new StringContent(document.Id.ToString()), "documentId");
            content.Add(new StringContent(securityDocument.Id.ToString()), "securityDocumentId");
            content.Add(new StringContent(securityDocument.EmployeeId.ToString()), "employeeId");
            content.Add(new StringContent(securityDocument.CompanyId.ToString()), "companyId");

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PostAsync($"/api/v1/securitydocument/insert", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentResponse = await response.Content.ReadFromJsonAsync(typeof(InsertDocumentResponse)) as InsertDocumentResponse ?? throw new ArgumentNullException();
            var result = await context.SecurityDocuments.AsNoTracking().Include(x => x.Documents.Where(x => x.Id == contentResponse.Id)).FirstOrDefaultAsync(x => x.Id == securityDocument.Id) ?? throw new ArgumentNullException();
            var documentResponse = result.Documents.First();
            Assert.Equal(Extension.PDF, documentResponse.Extension);
            Assert.Equal(typeof(Guid), documentResponse.Id.GetType());

            using var scope = _factory.Services.CreateScope();

            var blobService = scope.ServiceProvider.GetRequiredService<IBlobService>();
            var options = scope.ServiceProvider.GetRequiredService<SecurityDocumentsFilesOptions>();

            var stream = await blobService.DownloadAsync(documentResponse.GetNameWithExtension, options.DocumentsContainer, cancellationToken);

            Assert.NotNull(stream);
            Assert.True(stream.Length > 0);
            
        }


    }
}
