using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.CompanyCommands.CreateCompany;
using PeopleManagement.Application.Commands.DTO;
using PeopleManagement.IntegrationTests.Configs;
using System.Net;

namespace PeopleManagement.IntegrationTests.Tests
{
    [Collection(nameof(IntegrationTestCollection))]
    public class CompanyTest(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        // POST /company com payload válido cria a Company, retorna 200 + BaseDTO e persiste a entidade igual à esperada (ToCompany).
        [Fact]
        public async Task CreateCompanyWithSuccess()
        {
            var context = GetContext();
            var client = CreateClient();

            var command = new CreateCompanyCommand(
                "Lucas e Ryan Informática Ltda",
                "Lucas e Ryan Informática",
                "84.247.335/0001-07",
                "auditoria@lucaseryaninformaticaltda.com.br",
                "1637222844",
                "14093636",
                "Rua José Otávio de Oliveira",
                "776",
                "",
                "Parque dos Flamboyans",
                "Ribeirão Preto",
                "SP",
                "BRASIL"
            );

            client.InputHeaders();
            var response = await client.PostAsJsonAsync("/api/v1/company", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<BaseDTO>() ?? throw new ArgumentNullException();
            var result = await context.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToCompany(result.Id), result);
        }
    }
}
