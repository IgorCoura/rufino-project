using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;

namespace PeopleManagement.IntegrationTests.Tests
{
    public class RequireDocumentsTests(PeopleManagementWebApplicationFactory factory) : IClassFixture<PeopleManagementWebApplicationFactory>
    {
        private readonly PeopleManagementWebApplicationFactory _factory = factory;

        [Fact]
        public async Task CreateRequireDocumentsWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

           
            var documentTemplate = await context.InsertDocumentTemplate(cancellationToken);
            var role = await context.InsertRole(documentTemplate.CompanyId, cancellationToken);    
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateRequireDocumentsCommand(
                    role.Id,
                    documentTemplate.CompanyId,
                    "Contract Docs",
                    "Description Contract Docs",
                    [documentTemplate.Id]
                );


            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PostAsJsonAsync("/api/v1/requiredocuments/create", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateRequireDocumentsResponse)) as CreateRequireDocumentsResponse ?? throw new ArgumentNullException();
            var result = await context.RequireDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToRequireSecurityDocuments(result.Id), result);
        }

    }


}
