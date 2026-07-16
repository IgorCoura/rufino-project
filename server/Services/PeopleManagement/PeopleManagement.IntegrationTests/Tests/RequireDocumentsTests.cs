using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;
using System.Text.Json.Nodes;

namespace PeopleManagement.IntegrationTests.Tests
{
    [Collection(nameof(IntegrationTestCollection))]
    public class RequireDocumentsTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {

        // POST /requiredocuments cria a exigência de documentos associada a um cargo (Role) contendo um template; persiste igual à esperada (ToRequireDocuments).
        [Fact]
        public async Task CreateRequireDocumentsWithSuccess()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

           
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

        // GET /requiredocuments/association/{type} recupera as exigências de documentos por tipo de associação (Role);
        // com uma exigência semeada para o cargo, o endpoint responde 200 e devolve ao menos um item.
        [Fact]
        public async Task GetRequireDocumentsByAssociationType()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var documentTemplate = await context.InsertDocumentTemplate(cancellationToken);
            var role = await context.InsertRole(documentTemplate.CompanyId, cancellationToken);
            await context.InsertRequireDocuments(documentTemplate.CompanyId, role.Id, [documentTemplate.Id], cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            client.InputHeaders([documentTemplate.CompanyId]);
            var response = await client.GetAsync($"/api/v1/{documentTemplate.CompanyId}/requiredocuments/association/{AssociationType.Role.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var items = JsonNode.Parse(content)!.AsArray();
            Assert.NotEmpty(items);
        }

    }


}
