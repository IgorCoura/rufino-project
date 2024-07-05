using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.SecurityDocumentCommands.CreateDocument;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;

namespace PeopleManagement.IntegrationTests.Tests
{
    public class SecurityDocumentTests : IClassFixture<PeopleManagementWebApplicationFactory>
    {
        private readonly PeopleManagementWebApplicationFactory _factory;

        public SecurityDocumentTests(PeopleManagementWebApplicationFactory factory)
        {
            _factory = factory;
        }

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
            Assert.True(date.Equals(document.Date));
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

        }

    }
}
