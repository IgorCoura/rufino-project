using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterAddressEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterContactEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.CompleteAdmissionEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;

namespace PeopleManagement.IntegrationTests.Tests
{
    public class EmployeeTest : IClassFixture<PeopleManagementWebApplicationFactory>
    {
        private readonly PeopleManagementWebApplicationFactory _factory;

        public EmployeeTest(PeopleManagementWebApplicationFactory factory)
        {
            _factory = factory;
            
        }

        [Fact]
        public async Task CreateEmployeeWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var id = Guid.NewGuid();

            var employee = new CreateEmployeeCommand(company.Id, "Rosdevaldo Pereira");

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PostAsJsonAsync("/api/v1/employee/create", employee);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateEmployeeResponse)) as CreateEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();            
            Assert.Equal(employee.ToEmployee(result.Id), result);

        }

        [Fact]
        public async Task AlterAddressWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterAddressEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                "12603-130",
                "Rua Expedicionário Sebastião Ribeiro Guimarães",
                "949",
                "",
                "Vila Nunes",
                "Lorena",
                "SP",
                "Brasil"
                );

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/address", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterAddressEmployeeResponse)) as AlterAddressEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToAddress(), result.Address);
        }

        [Fact]
        public async Task AlterContactWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterContactEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                "11 99765-7890",
                "email@topsemail.com"
                );

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/contact", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterContactEmployeeResponse)) as AlterContactEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToContact(), result.Contact);
        }

        [Fact]
        public async Task AlterDependentWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithOneDependent(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterDependentEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                employee.Dependents.First().Name.Value,
                new DependentModelAlterDependentEmployeeCommand(
                    "Roberto Matias Kaique",
                    new IdCardModelAlterDependentEmployeeCommand(
                        "289.598.500-63",
                        "Pamela Matias Kaika",
                        "Andelou Araujo Kaika",
                        "Ribeirão Preto",
                        "SP",
                        "brasileiro",
                        DateOnly.Parse("2000/01/23")
                        ),
                    1,
                    1
                    )
                );

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/dependent", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterDependentEmployeeResponse)) as AlterDependentEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            var expected = command.CurrentDepentent.ToDependent();
            Assert.Single(result.Dependents);
            Assert.Equal(expected, result.Dependents.FirstOrDefault(x => x.Name.Equals(expected.Name)));
        }


        [Fact]
        public async Task CompleteEmployeeAdmissionWithSuccess()
        {

            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithAllInfoToAdmission(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CompleteAdmissionEmployeeCommand(employee.Id, employee.CompanyId, "RU1902", EmploymentContactType.CLT.Id);
            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/complete/admission", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CompleteAdmissionEmployeeResponse)) as CompleteAdmissionEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(Status.Active, result.Status);
        }


    }
}
