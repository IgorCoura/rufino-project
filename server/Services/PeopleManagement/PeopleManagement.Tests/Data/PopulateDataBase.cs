using PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using PeopleManagement.Infra.Context;
using AddressCompany = PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Address;
using AddressWorkplace = PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Address;

namespace PeopleManagement.Tests.Data
{
    public static class PopulateDataBase
    {
        public static async Task<Guid> InsertCompany(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            var company = Company.Create(
            id,
            "Lucas e Ryan Informática Ltda",
            "Lucas e Ryan Informática",
            "82161379000186",
            Contact.Create(
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

            return id;
        }

        public static async Task<Guid> InsertRole(this PeopleManagementContext context, CancellationToken cancellationToken = default)
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

            return roleId;
        }

        public static async Task<Guid> InsertWorkplace(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var workplaceId = Guid.NewGuid();

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

            return workplaceId;
        }

        public static async Task<Guid> InsertEmployeeWithMinimalInfos(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var companyId = await context.InsertCompany(cancellationToken);

            var id = Guid.NewGuid();

            var employee = Employee.Create(id, companyId, "Rosdevaldo Pereira");

            await context.Employees.AddAsync(employee, cancellationToken);

            return id;
        }
    }
}
