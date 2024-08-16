using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterAddressEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterContactEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterIdCardEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterPersonalInfoEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterRoleEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterVoteIdEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.CompleteAdmissionEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.FinishedContractEmployee;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;
using System.Threading;
using NameEmployee = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Name;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace PeopleManagement.IntegrationTests.Tests
{
    public class EmployeeTest(PeopleManagementWebApplicationFactory factory) : IClassFixture<PeopleManagementWebApplicationFactory>
    {
        private readonly PeopleManagementWebApplicationFactory _factory = factory;

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
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();            
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
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToAddress(), result.Address);
            var archives = await context.Archives.AsNoTracking().Where(x => x.OwnerId == content.Id).ToListAsync(cancellationToken);           
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
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToContact(), result.Contact);
        }

        [Fact]
        public async Task CreateDependentWithSuccess()
        {
            var cancellationToken = new CancellationToken();
            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateDependentEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                "Roberto Matias Saveiro Kaique",
                new IdCardModelCreateDependentEmployeeCommand(
                    "289.598.500-63",
                    "Pamela Matias Saveiro Kaika",
                    "Andelou Araujo Saveiro Kaika",
                    "Ribeirão Pires",
                    "SP",
                    "brasileira",
                    DateOnly.Parse("1995/01/23")
                    ),
                1,
                2
                );

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PostAsJsonAsync("/api/v1/employee/create/dependent", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateDependentEmployeeResponse)) as CreateDependentEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Single(result.Dependents);
            Assert.Equal(command.ToDependent(), result.Dependents.First(x => x.Name.Equals((NameEmployee)command.Name)));
            await CheckRequestDocumentEvent(context, RequestFilesEvent.SpouseDocument(employee.Id, employee.CompanyId), cancellationToken);
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
            await CheckRequestDocumentEvent(context, RequestFilesEvent.ChildDocument(employee.Id, employee.CompanyId), cancellationToken);
        }

        [Fact]
        public async Task AlterIdCardWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterIdCardEmployeeCommand(
                    employee.Id,
                    employee.CompanyId,
                    "289.598.500-63",
                    "Pamela Matias Kaika",
                    "Andelou Araujo Kaika",
                    "Ribeirão Preto",
                    "SP",
                    "brasileiro",
                    DateOnly.Parse("2000/01/23")
                );


            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/idcard", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterIdCardEmployeeResponse)) as AlterIdCardEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToIdCard(), result.IdCard);
        }

        [Fact]
        public async Task AlterMedicalAdmissionExamWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterMedicalAdmissionExamEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                DateOnly.Parse("2024/04/01"),
                DateOnly.Parse("2025/04/01"));

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/MedicalAdmissionExam", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterMedicalAdmissionExamEmployeeResponse)) as AlterMedicalAdmissionExamEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToMedicalAdmissionExam(), result.MedicalAdmissionExam);
        }


        [Fact]
        public async Task AlterMilitaryDocumentWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterMilitarDocumentEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                "33456888565",
                "Reservista");

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/MilitarDocument", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterMilitarDocumentEmployeeResponse)) as AlterMilitarDocumentEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToMilitaryDocument(), result.MilitaryDocument);
        }

        [Fact]
        public async Task AlterNameWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterNameEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                "Roberto de Maione");

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/name", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterNameEmployeeResponse)) as AlterNameEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.Name, result.Name);
        }

        [Fact]
        public async Task AlterPersonalInfoWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterPersonalInfoEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                new DeficiencyModel([], ""),
                1,
                1,
                1,
                1
                );

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/personalinfo", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterPersonalInfoEmployeeResponse)) as AlterPersonalInfoEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToPersonalInfo(), result.PersonalInfo);
        }

        [Fact]
        public async Task AlterRoleWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            var role = await context.InsertRole(employee.CompanyId, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterRoleEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                role.Id
                );

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/role", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterRoleEmployeeResponse)) as AlterRoleEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.RoleId, result.RoleId);
        }

        [Fact]
        public async Task AlterVoteIdWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterVoteIdEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                "354627230167"
                );

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/voteid", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterVoteIdEmployeeResponse)) as AlterVoteIdEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.VoteIdNumber, result.VoteId);
        }

        [Fact]
        public async Task AlterWorkPlaceWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            var workplace = await context.InsertWorkplace(employee.CompanyId, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterWorkPlaceEmployeeCommand(
                employee.Id,
                employee.CompanyId,
                workplace.Id
                );

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/alter/workplace", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterWorkPlaceEmployeeResponse)) as AlterWorkPlaceEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.WorkPlaceId, result.WorkPlaceId);
        }

        [Fact]
        public async Task CompleteEmployeeAdmissionWithSuccess()
        {

            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var aaa1 = await context.Archives.ToListAsync();

            var employee = await context.InsertEmployeeWithAllInfoToAdmission(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
            var command = new CompleteAdmissionEmployeeCommand(employee.Id, employee.CompanyId, "RU1902", dateNow, EmploymentContactType.CLT.Id);
            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/complete/admission", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CompleteAdmissionEmployeeResponse)) as CompleteAdmissionEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(Status.Active, result.Status);
            Assert.Equal(command.Registration, result.Registration?.Value);
            var contract = result.Contracts.OrderBy(x => x.InitDate).Last();
            Assert.Equal(command.ContractType, contract.ContractType.Id);
            Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), contract.InitDate);
            
            var securityDocument = await context.Documents.AsNoTracking().FirstAsync(x  => x.EmployeeId == employee.Id && x.CompanyId == employee.CompanyId);
            Assert.Equal(Domain.AggregatesModel.DocumentAggregate.DocumentStatus.RequiredDocument , securityDocument.Status);

            await CheckRequestDocumentEvent(context, RequestFilesEvent.CompleteAdmissionFiles(employee.Id, employee.CompanyId), cancellationToken);
        }

        [Fact]
        public async Task FinishedContractEmployeeWithSuccess()
        {

            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeActive(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new FinishedContractEmployeeCommand(employee.Id, employee.CompanyId, DateOnly.Parse("30/05/2024"));
            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/employee/finished/contract", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(FinishedContractEmployeeResponse)) as FinishedContractEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(Status.Inactive, result.Status);
            var contract = result.Contracts.OrderBy(x => x.FinalDate).Last();
            Assert.Equal(command.FinishDateContract, contract.FinalDate);
            await CheckRequestDocumentEvent(context, RequestFilesEvent.MedicalDismissalExam(employee.Id, employee.CompanyId), cancellationToken);
        }


        private async static Task CheckRequestDocumentEvent(PeopleManagementContext context, RequestFilesEvent documentsEvent, CancellationToken cancellationToken = default)
        {
            var archivesCategories = await context.ArchiveCategories.AsNoTracking().ToListAsync(cancellationToken);
            archivesCategories = archivesCategories.Where(x => x.ListenEventsIds.Contains(documentsEvent.Id)).ToList();
            var archives = await context.Archives.AsNoTracking().Where(x => x.OwnerId == documentsEvent.OwnerId && x.CompanyId == documentsEvent.CompanyId && x.Status == ArchiveStatus.RequiresFile).ToListAsync(cancellationToken);

            foreach(var category in archivesCategories)
            {
                var archivesCat = archives.Where(x => x.CategoryId == category.Id).ToList();
                Assert.Single(archivesCat);
                var archive = archivesCat.First();
                Assert.Equal(ArchiveStatus.RequiresFile, archive.Status);
            }          
        }
    }
}
