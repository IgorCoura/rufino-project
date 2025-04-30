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
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using static PeopleManagement.IntegrationTests.Data.PopulateDataBase;
using static PeopleManagement.IntegrationTests.Data.PopulateDatabaseDirectly;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;

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

            var company = CompanyDAO.CreateFix();
            await company.InsertInDB(context, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var id = Guid.NewGuid();

            var employee = new CreateEmployeeModel("Rosdevaldo Pereira");

            client.InputHeaders([company.Id]);
            var response = await client.PostAsJsonAsync($"/api/v1/{company.Id}/employee", employee);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateEmployeeResponse)) as CreateEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();            
            Assert.Equal(employee.ToCommand(company.Id).ToEmployee(result.Id), result);

        }

        [Fact]
        public async Task AlterAddressWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterAddressEmployeeModel(
                employee.Id,
                "12603-130",
                "Rua Expedicionário Sebastião Ribeiro Guimarães",
                "949",
                "",
                "Vila Nunes",
                "Lorena",
                "SP",
                "Brasil"
                );

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/address", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterAddressEmployeeResponse)) as AlterAddressEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToCommand(employee.CompanyId).ToAddress(), result.Address);
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

            var command = new AlterContactEmployeeModel(
                employee.Id,
                "11 99765-7890",
                "email@topsemail.com"
                );

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/contact", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterContactEmployeeResponse)) as AlterContactEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToCommand(employee.CompanyId).ToContact(), result.Contact);
        }

        [Fact]
        public async Task CreateDependentWithSuccess()
        {
            var cancellationToken = new CancellationToken();
            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateDependentEmployeeModel(
                employee.Id,
                "Roberto Matias Saveiro Kaique",
                new IdCardModelCreateDependentEmployeeModel(
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

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/dependent/create", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateDependentEmployeeResponse)) as CreateDependentEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Single(result.Dependents);
            Assert.Equal(command.ToCommand(employee.CompanyId).ToDependent(), result.Dependents.First(x => x.Name.Equals((NameEmployee)command.Name)));
            await CheckRequestDocumentEvent(context, EmployeeEvent.DependentSpouseChangeEvent(employee.Id, employee.CompanyId), cancellationToken);
        }

        [Fact]
        public async Task AlterDependentWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithOneDependent(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterDependentEmployeeModel(
                employee.Id,
                employee.Dependents.First().Name.Value,
                new DependentModelAlterDependentEmployeeModel(
                    "Roberto Matias Kaique",
                    new IdCardModelAlterDependentEmployeeModel(
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

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/dependent/edit", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterDependentEmployeeResponse)) as AlterDependentEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            var expected = command.CurrentDependent.ToDependent();
            Assert.Single(result.Dependents);
            Assert.Equal(expected, result.Dependents.FirstOrDefault(x => x.Name.Equals(expected.Name)));
            await CheckRequestDocumentEvent(context, EmployeeEvent.DependentChildChangeEvent(employee.Id, employee.CompanyId), cancellationToken);
        }

        [Fact]
        public async Task AlterIdCardWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterIdCardEmployeeModel(
                    employee.Id,
                    "289.598.500-63",
                    "Pamela Matias Kaika",
                    "Andelou Araujo Kaika",
                    "Ribeirão Preto",
                    "SP",
                    "brasileiro",
                    DateOnly.Parse("2000/01/23")
                );


            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/idcard", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterIdCardEmployeeResponse)) as AlterIdCardEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToCommand(employee.CompanyId).ToIdCard(), result.IdCard);
        }

        [Fact]
        public async Task AlterMedicalAdmissionExamWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterMedicalAdmissionExamEmployeeModel(
                employee.Id,
                DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
                DateOnly.FromDateTime(DateTime.Now.AddDays(335)));

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/MedicalAdmissionExam", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterMedicalAdmissionExamEmployeeResponse)) as AlterMedicalAdmissionExamEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToCommand(employee.CompanyId).ToMedicalAdmissionExam(), result.MedicalAdmissionExam);
        }


        [Fact]
        public async Task AlterMilitaryDocumentWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterMilitarDocumentEmployeeModel(
                employee.Id,
                "33456888565",
                "Reservista");

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/MilitarDocument", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterMilitarDocumentEmployeeResponse)) as AlterMilitarDocumentEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToCommand(employee.CompanyId).ToMilitaryDocument(), result.MilitaryDocument);
        }

        [Fact]
        public async Task AlterNameWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterNameEmployeeModel(
                employee.Id,
                "Roberto de Maione");

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/name", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterNameEmployeeResponse)) as AlterNameEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.Name, result.Name.ToString(), StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AlterPersonalInfoWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeWithMinimalInfos(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterPersonalInfoEmployeeModel(
                employee.Id,
                new DeficiencyModel([], ""),
                1,
                1,
                1,
                1
                );

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/personalinfo", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AlterPersonalInfoEmployeeResponse)) as AlterPersonalInfoEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToCommand(employee.CompanyId).ToPersonalInfo(), result.PersonalInfo);
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

            var command = new AlterRoleEmployeeModel(
                employee.Id,
                role.Id
                );

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/role", command);

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

            var command = new AlterVoteIdEmployeeModel(
                employee.Id,
                "354627230167"
                );

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/voteid", command);

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

            var command = new AlterWorkPlaceEmployeeModel(
                employee.Id,
                workplace.Id
                );

            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/workplace", command);

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

            var employee = await context.InsertEmployeeWithAllInfoToAdmission(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
            var command = new CompleteAdmissionEmployeeModel(employee.Id,"RU1902", dateNow, EmploymentContractType.CLT.Id);
            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/admission/complete", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CompleteAdmissionEmployeeResponse)) as CompleteAdmissionEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(Status.Active, result.Status);
            Assert.Equal(command.Registration, result.Registration?.Value);
            var contract = result.Contracts.OrderBy(x => x.InitDate).Last();
            Assert.Equal(command.ContractType, contract.ContractType.Id);
            Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), contract.InitDate);


            await CheckRequestDocumentEvent(context, EmployeeEvent.CompleteAdmissionEvent(employee.Id, employee.CompanyId), cancellationToken);
        }

        [Fact]
        public async Task FinishedContractEmployeeWithSuccess()
        {

            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeActive(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new FinishedContractEmployeeModel(employee.Id, DateOnly.Parse("30/05/2024"));
            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/contract/finished", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(FinishedContractEmployeeResponse)) as FinishedContractEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(Status.Inactive, result.Status);
            var contract = result.Contracts.OrderBy(x => x.FinalDate).Last();
            Assert.Equal(command.FinishDateContract, contract.FinalDate);
            await CheckRequestDocumentEvent(context, EmployeeEvent.DemissionalExamRequestEvent(employee.Id, employee.CompanyId), cancellationToken);
        }

        [Fact]
        public async Task ChangeEmployeeRoleWhenEmployeeIsActiveWithSuccess()
        {

            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var employee = await context.InsertEmployeeActive(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var newRole = await context.InsertRole(employee.CompanyId, cancellationToken);
            var newDocumentTemplate = await context.InsertDocumentTemplate(employee.CompanyId, cancellationToken);
            var newRequireDocument = await context.InsertRequireDocuments(employee.CompanyId, newRole.Id, [newDocumentTemplate.Id], cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new AlterRoleEmployeeCommand(employee.Id, employee.CompanyId, newRole.Id);
            client.InputHeaders([employee.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{employee.CompanyId}/employee/role", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(FinishedContractEmployeeResponse)) as FinishedContractEmployeeResponse ?? throw new ArgumentNullException();
            var result = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(newRole.Id, result.RoleId);

            var document = await context.Documents.Include(x => x.DocumentsUnits).AsNoTracking()
                    .Where(x => x.CompanyId == employee.CompanyId && x.EmployeeId == employee.Id)
                    .ToListAsync();

            Assert.Single(document);

            var doc = document.First();

            Assert.Equal(newRequireDocument.Id, doc.RequiredDocumentId);

            Assert.Single(doc.DocumentsUnits);
            var documentUnit = doc.DocumentsUnits.First();
            Assert.Equal(DocumentUnitStatus.Pending, documentUnit.Status);
        }


        private async static Task CheckRequestDocumentEvent(PeopleManagementContext context, EmployeeEvent @event, CancellationToken cancellationToken = default)
        {
            var archivesCategories = await context.ArchiveCategories.AsNoTracking().Where(x => x.CompanyId == @event.CompanyId)
                .ToListAsync(cancellationToken);
            archivesCategories = archivesCategories.Where(x => x.ListenEventsIds.Contains(@event.Id)).ToList();
            var archives = await context.Archives.AsNoTracking().Where(x => x.OwnerId == @event.EmployeeId 
            && x.CompanyId == @event.CompanyId && x.Status == ArchiveStatus.RequiresFile).ToListAsync(cancellationToken);

            foreach (var category in archivesCategories)
            {
                var archivesCat = archives.Where(x => x.CategoryId == category.Id).ToList();
                Assert.Single(archivesCat);
                var archive = archivesCat.First();
                Assert.Equal(ArchiveStatus.RequiresFile, archive.Status);
            }


            var requireDocuments = await context.RequireDocuments.AsNoTracking().Where(x => x.CompanyId == @event.CompanyId 
            && x.ListenEvents.Any(l => l.EventId == @event.Id)).ToListAsync(cancellationToken);

            foreach (var requireDocument in requireDocuments)
            {
                var document = await context.Documents.Include(x => x.DocumentsUnits).AsNoTracking()
                    .Where(x => x.RequiredDocumentId == requireDocument.Id && x.CompanyId == @event.CompanyId && x.EmployeeId == @event.EmployeeId)
                    .ToListAsync();

                Assert.Single(document);

                var doc = document.First();

                Assert.Single(doc.DocumentsUnits);
                var documentUnit = doc.DocumentsUnits.First();
                Assert.Equal(DocumentUnitStatus.Pending, documentUnit.Status);
            }


        }
    }
}
