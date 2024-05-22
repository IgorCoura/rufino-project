using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.CompanyCommands.CreateCompany;
using PeopleManagement.Application.Commands.DTO;
using PeopleManagement.Tests.Configs;
using System.Net;

namespace PeopleManagement.Tests.IntegrationTests
{
    public class CompanyTest
    {
        [Fact]
        public async Task CreateCompanyWithSuccess()
        {
            //Arrange 
            using var factory = new PeopleManagementWebApplicationFactory();
            var client = factory.CreateClient();
            using var context = factory.GetContext();

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());

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

            var response = await client.PostAsJsonAsync("/api/v1/company", company);

            //Assert

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(BaseDTO)) as BaseDTO ?? throw new ArgumentNullException();
            var result = await context.Companies.FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(company.ToCompany(result.Id), result);
        }
    }
}
