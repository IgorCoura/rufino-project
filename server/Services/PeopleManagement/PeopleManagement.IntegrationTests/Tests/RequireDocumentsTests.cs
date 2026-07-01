using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;

namespace PeopleManagement.IntegrationTests.Tests
{
    [Collection(nameof(IntegrationTestCollection))]
    public class RequireDocumentsTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {

        // POST /requiredocuments cria a exigência de documentos associada a um cargo (Role) contendo um template; persiste igual à esperada (ToRequireDocuments).
        [Fact]
        public async Task CreateRequireDocumentsWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

           
            var documentTemplate = await context.InsertDocumentTemplate(cancellationToken);
            var role = await context.InsertRole(documentTemplate.CompanyId, cancellationToken);    
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateRequireDocumentsModel(
                    [role.Id],
                    AssociationType.Role.Id,
                    "Contract Docs",
                    "Description Contract Docs",
                    [],
                    [documentTemplate.Id]
                );


            client.InputHeaders([documentTemplate.CompanyId]);
            var response = await client.PostAsJsonAsync($"/api/v1/{documentTemplate.CompanyId}/requiredocuments", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateRequireDocumentsResponse)) as CreateRequireDocumentsResponse ?? throw new ArgumentNullException();
            var result = await context.RequireDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToCommand(documentTemplate.CompanyId).ToRequireDocuments(result.Id), result);
        }

        // GET /requiredocuments/association/1 recupera as exigências de documentos por tipo de associação.
        // ATENÇÃO: teste sem asserções — apenas exercita o endpoint (não valida status nem corpo da resposta).
        [Fact]
        public async Task GetRequireDocumentsByAssociationType()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();


            var documentTemplate = await context.InsertDocumentTemplate(cancellationToken);
            var role = await context.InsertRole(documentTemplate.CompanyId, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            client.InputHeaders([documentTemplate.CompanyId]);
            var response = await client.GetAsync($"/api/v1/{documentTemplate.CompanyId}/requiredocuments/association/1");

            var content = await response.Content.ReadAsStringAsync();

        }

    }


}
