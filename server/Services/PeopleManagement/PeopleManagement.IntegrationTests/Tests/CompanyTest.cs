using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.CompanyCommands.CreateCompany;
using PeopleManagement.Application.Commands.DTO;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using System.Net;

namespace PeopleManagement.IntegrationTests.Tests
{
    public class CompanyTest : IClassFixture<PeopleManagementWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly PeopleManagementWebApplicationFactory _factory;
        private readonly PeopleManagementContext _context;

        public CompanyTest(PeopleManagementWebApplicationFactory factory)
        {
            _factory = factory;
            _context = _factory.GetContext();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateCompanyWithSuccess()
        {
            //Arrange 

            _client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());

            var context = _factory.GetContext();

            var company = new CreateCompanyCommand(
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

            //Act

            var response = await _client.PostAsJsonAsync("/api/v1/company", company);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(BaseDTO)) as BaseDTO ?? throw new ArgumentNullException();
            var result = await context.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(company.ToCompany(result.Id), result);
        }
    }
}
