using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DTO;
using PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Tests.Configs;
using PeopleManagement.Tests.Data;
using System.Net;

namespace PeopleManagement.Tests.IntegrationTests
{
    public class EmployeeTest
    {
        [Fact]
        public async Task CreateEmployee()
        {
            var cancellationToken = new CancellationToken();

            using var factory = new PeopleManagementWebApplicationFactory();
            var client = factory.CreateClient();
            using var context = factory.GetContext();

            var companyId = await context.InsertCompany(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var id = Guid.NewGuid();

            var employee = new CreateEmployeeCommand(companyId, "Rosdevaldo Pereira");

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PostAsJsonAsync("/api/v1/employee", employee);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateEmployeeResponse)) as CreateEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(employee.ToEmployee(result.Id), result);
        }

        [Fact]
        public async Task CompleteEmployeeAdmissionWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            using var factory = new PeopleManagementWebApplicationFactory();
            var client = factory.CreateClient();
            using var context = factory.GetContext();

            var employeeId = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            Assert.Fail();
        }
    }
}
 