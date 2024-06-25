using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using PeopleManagement.Infra.Context;
using AddressCompany = PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Address;
using AddressWorkplace = PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Address;
using ContactCompany = PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Contact;
using EmplyeeAddress = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Address;
using FileArchive = PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.File;

namespace PeopleManagement.IntegrationTests.Data
{
    public static class PopulateDataBase
    {
        public static async Task<Company> InsertCompany(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            var company = Company.Create(
            id,
            "Lucas e Ryan Informática Ltda",
            "Lucas e Ryan Informática",
            "82161379000186",
            ContactCompany.Create(
                "auditoria@lucaseryaninformaticaltda.com.br",
                "1637222844"),
            AddressCompany.Create(
                "14093636",
                "Rua José Otávio de Oliveira",
                "776",
                "",
                "Parque dos Flamboyans",
                "Ribeirão Preto",
                "SP",
                "BRASIL")
            );

            await context.Companies.AddAsync(company, cancellationToken);

            return company;
        }

        public static async Task<Role> InsertRole(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var departmentId = Guid.NewGuid();
            var departament = Department.Create(departmentId, "Hidraulica", "Hidraulica");
            await context.Departments.AddAsync(departament, cancellationToken);

            var postionId = Guid.NewGuid();
            var position = Position.Create(postionId, "Encanador", "Encanador", "738298", departmentId);
            await context.Positions.AddAsync(position, cancellationToken);
            
            var roleId = Guid.NewGuid();
            var role = Role.Create(roleId, "Encanador Senior", "Encanador Com Experiencia", "738298", Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.Real, "10.55"), "Por Hora"), postionId);
            await context.Roles.AddAsync(role, cancellationToken);

            return role;
        }

        public static async Task<Workplace> InsertWorkplace(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {

            var workplace = Workplace.Create("Eleve", AddressWorkplace.Create(
                "14093636",
                "Rua José Otávio de Oliveira",
                "776",
                "",
                "Parque dos Flamboyans",
                "Ribeirão Preto",
                "SP",
                "BRASIL"));

            await context.Workplaces.AddAsync(workplace, cancellationToken);

            return workplace;
        }

        public static async Task<Employee> InsertEmployeeWithMinimalInfos(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var company = await context.InsertCompany(cancellationToken);

            var id = Guid.NewGuid();

            var employee = Employee.Create(id, company.Id, "Rosdevaldo Pereira");

            await context.Employees.AddAsync(employee, cancellationToken);

            return employee;
        }

        public static async Task<Employee> InsertEmployeeWithOneDependent(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var company = await context.InsertCompany(cancellationToken);

            var id = Guid.NewGuid();

            var employee = Employee.Create(id, company.Id, "Rosdevaldo Pereira");
            employee.AddDependent(
                Dependent.Create(
                    "Roberto Kaique",
                    IdCard.Create(
                        "630.354.970-52",
                        "Bragrku Aldase",
                        "Shobrowu Voen",
                        "Bauru",
                        "SP",
                        "brasileiro",
                        DateOnly.Parse("1995/04/30")
                    ),
                    1,
                    1)
                );

            await context.Employees.AddAsync(employee, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await context.SendRequiresFiles(employee.Id, employee.CompanyId, cancellationToken);

            return employee;
        }

        public static async Task<Employee> InsertEmployeeWithAllInfoToAdmission(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var company = await context.InsertCompany(cancellationToken);
            var id = Guid.NewGuid();
            var employee = Employee.Create(id, company.Id, "Rosdevaldo Pereira");
            var role = await context.InsertRole(cancellationToken);
            var workplace = await context.InsertWorkplace(cancellationToken);

            employee.RoleId = role.Id;
            employee.WorkPlaceId = workplace.Id;

            employee.Address = EmplyeeAddress.Create("69015-756", "Rua 11", "936", "", "Colônia Terra Nova", "Manaus", "Amazonia", "Brasil");


            employee.PersonalInfo = PersonalInfo.Create(Deficiency.Create("", []), MaritalStatus.Single, Gender.MALE, Ethinicity.White, EducationLevel.CompleteHigher);
            employee.IdCard = IdCard.Create("216.456.330-12", "Maria Silva", "Marcio Andrade", "Suzano", "São Paulo", "Brasileiro", DateOnly.Parse("2000/01/01"));
            employee.VoteId = VoteId.Create("281662310124");

            employee.MedicalAdmissionExam = MedicalAdmissionExam.Create(DateOnly.Parse("2024/04/20"), DateOnly.Parse("2025/04/20"));

            employee.MilitaryDocument = MilitaryDocument.Create("2312312312", "Rersevista");

            await context.Employees.AddAsync(employee, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            await context.SendRequiresFiles(employee.Id, employee.CompanyId, cancellationToken);

            return employee;
        }

        public static async Task<Employee> InsertEmployeeActive(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var company = await context.InsertCompany(cancellationToken);
            var id = Guid.NewGuid();
            var employee = Employee.Create(id, company.Id, "Rosdevaldo Pereira");
            var role = await context.InsertRole(cancellationToken);
            var workplace = await context.InsertWorkplace(cancellationToken);

            employee.RoleId = role.Id;
            employee.WorkPlaceId = workplace.Id;

            employee.Address = EmplyeeAddress.Create("69015-756", "Rua 11", "936", "", "Colônia Terra Nova", "Manaus", "Amazonia", "Brasil");


            employee.PersonalInfo = PersonalInfo.Create(Deficiency.Create("", []), MaritalStatus.Single, Gender.MALE, Ethinicity.White, EducationLevel.CompleteHigher);
            employee.IdCard = IdCard.Create("216.456.330-12", "Maria Silva", "Marcio Andrade", "Suzano", "São Paulo", "Brasileiro", DateOnly.Parse("2000/01/01"));
            employee.VoteId = VoteId.Create("281662310124");

            employee.MedicalAdmissionExam = MedicalAdmissionExam.Create(DateOnly.Parse("2024/04/20"), DateOnly.Parse("2025/04/20"));

            employee.MilitaryDocument = MilitaryDocument.Create("2312312312", "Rersevista");

            var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);

            employee.CompleteAdmission("RU123", dateNow, EmploymentContactType.CLT);

            await context.Employees.AddAsync(employee, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await context.SendRequiresFiles(employee.Id, employee.CompanyId, cancellationToken);

            return employee;
        }

        public static async Task SendRequiresFiles(this PeopleManagementContext context, Guid ownerId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var archives = await context.Archives.Where(x => x.OwnerId == ownerId && x.CompanyId == companyId && x.Status == ArchiveStatus.RequiresFile).ToListAsync(cancellationToken);
            archives.ForEach(x => x.AddFile(FileArchive.CreateWithoutVerification(Guid.NewGuid().ToString(), Extension.PDF, DateTime.UtcNow)));
        }
    }
}
