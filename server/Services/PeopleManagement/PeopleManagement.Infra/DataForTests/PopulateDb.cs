using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using CompanyAddress = PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Address;
using CompanyContact = PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Contact;
using WorkPlaceAddress = PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Address;
using EmployeeAddress = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Address;
using EmployeeContact = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Contact;
using Employee = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Employee;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.DataForTests
{
    public static class PopulateDb
    {
        public async static Task Populate(PeopleManagementContext context)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (env != null && env.Equals("Development"))
            {
                try
                {
                    var companies = CreateCompanies();
                    await context.Companies.AddRangeAsync(companies);
                    var departamets = CreateDepartments(companies[0].Id);
                    await context.Departments.AddRangeAsync(departamets);
                    var positions = CreatePositions(departamets[0].Id, companies[0].Id);
                    await context.Positions.AddRangeAsync(positions);
                    var roles = CreateRoles(positions[0].Id, companies[0].Id);
                    await context.Roles.AddRangeAsync(roles);
                    var workplaces = CreateWorkPlaces(companies[0].Id);
                    await context.Workplaces.AddRangeAsync(workplaces);
                    var employees = CreateEmployees(roles[0].Id, workplaces[0].Id, companies[0].Id);
                    await context.Employees.AddRangeAsync(employees);
                    await context.SaveChangesWithoutDispatchEventsAsync();
                }
                catch { }
            }
        }

        public static Company[] CreateCompanies()
        {
            var comapnies = new[] {
                Company.Create(
                    Guid.NewGuid(),
                    "Anderson e Fátima Entulhos ME",
                    "Anderson e Fátima Entulhos",
                    "83.559.705/0001-70",
                    CompanyContact.Create("compras@andersonefatimaentulhosme.com.br", "(17) 98301-7063"),
                    CompanyAddress.Create(
                        "14704-066",
                        "Rua Botafogo",
                        "504",
                        "",
                        "Jardim 3 Marias",
                        "Bebedouro",
                        "São Paulo",
                        "Brasil"
                    ) ),
                Company.Create(
                    Guid.NewGuid(),
                    "Laura e Maya Mudanças Ltda",
                    "Laura e Maya Mudanças",
                    "78.718.423/0001-39",
                    CompanyContact.Create("ouvidoria@lauraemayamudancasltda.com.br", "(16) 98767-5740"),
                    CompanyAddress.Create(
                        "14871-730",
                        "Travessa Guilherme Nascibem",
                        "617",
                        "",
                        "Parque do Trevo",
                        "Jaboticabal",
                        "São Paulo",
                        "Brasil"
                ) ),
            };
            return comapnies;
        }

        public static Department[] CreateDepartments(Guid companyId)
        {
            var departments = new[]
            {
                Department.Create(Guid.NewGuid(),"Tecnologia", "Departamento de tecnologia.", companyId),
                Department.Create(Guid.NewGuid(), "Recursos Humanos", "Departamento de recursos humanos.", companyId),
            };
            return departments;
        }

        public static Position[] CreatePositions(Guid departamentId, Guid companydId)
        {
            var positions = new[]
            {
                Position.Create(Guid.NewGuid(), "Engenheiro de Software", "Resposavel por criar softwares", "123454", departamentId, companydId),
                Position.Create(Guid.NewGuid(), "Engenheiro de dados", "Resposavel por analisar dados", "123465", departamentId, companydId),
            };
            return positions;
        }

        public static Role[] CreateRoles(Guid positionId, Guid companydId)
        {
            var roles = new[]
            {
                Role.Create(Guid.NewGuid(), "Engenheiro de Software Junior", "Resposavel por criar softwares", "123455" , Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "30"), "Pagamento Eng Junior" ), positionId, companydId),
                Role.Create(Guid.NewGuid(), "Engenheiro de Software Senior", "Resposavel por criar softwares", "123456", Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "30"), "Pagamento Eng Senior" ), positionId, companydId),
            };
            return roles;
        }

        public static Workplace[] CreateWorkPlaces(Guid companyId)
        {
            var workplaces = new[]
            {
                Workplace.Create(
                    Guid.NewGuid(), 
                    "Escritorio Matriz", 
                    WorkPlaceAddress.Create(
                        "14704-066",
                        "Rua Botafogo",
                        "504",
                        "",
                        "Jardim 3 Marias",
                        "Bebedouro",
                        "São Paulo",
                        "Brasil"), 
                    companyId),
            };

            return workplaces;
        }

        public static Employee[] CreateEmployees(Guid roleId, Guid workplaceId, Guid companyId)
        {
            var employees = new List<Employee>();

            employees.Add(
                FactoryEmployee(Guid.NewGuid(), companyId, "Rogerio Marques", roleId, workplaceId)
                );
            employees.Add(
                FactoryEmployee(
                    Guid.NewGuid(),
                    companyId,
                    "Andrea Clara Valentina Jesus",
                    roleId,
                    workplaceId,
                    EmployeeAddress.Create(
                        "29170-196",
                        "Avenida Diamantina",
                        "126",
                        "",
                        "Nova Carapina II",
                        "Serra",
                        "ES",
                        "Brasil"
                        ),
                    EmployeeContact.Create(
                        "andrea_clara_jesus@inpa.gov.br",
                        "(27) 99431-6916"
                        ),
                    PersonalInfo.Create(
                        Deficiency.Create("", []),
                        MaritalStatus.Single,
                        Gender.FEMALE,
                        Ethinicity.White,
                        EducationLevel.CompleteHigher
                        ),
                    IdCard.Create(
                        "455.800.699-36",
                        "Teresinha Isabelle Débora",
                        "Raul Lucca Jesus",
                        "Serra",
                        "ES",
                        "brasileira",
                        DateOnly.Parse("1993-07-23")
                        ),
                    VoteId.Create("113377750140"),
                    MedicalAdmissionExam.Create(DateOnly.Parse("2024-08-30"), DateOnly.Parse("2025-08-30")),
                    militaryDocument: null,
                    "TC0001",
                    DateOnly.Parse("2024-09-01"),
                    EmploymentContactType.CLT
                    ));

            return employees.ToArray();

        }

        private static Employee FactoryEmployee(Guid id, Guid companyId, string Name, Guid? roleId = null, Guid? workplaceId = null,
            EmployeeAddress? address = null, EmployeeContact? contact = null, PersonalInfo? personalInfo = null, IdCard? idCard = null, 
            VoteId? voteId = null,  MedicalAdmissionExam? medicalAdmissionExam = null, MilitaryDocument? militaryDocument = null, 
            string? registration = null, DateOnly? dateInitContract = null, EmploymentContactType? contractType = null)
        {
            var employee = Employee.Create(id, companyId, Name);
            

            try
            {
                employee.RoleId = roleId;
                employee.WorkPlaceId = workplaceId;
                employee.Address = address;
                employee.Contact = contact;
                employee.PersonalInfo = personalInfo;
                employee.IdCard = idCard;
                employee.VoteId = voteId;
                employee.MedicalAdmissionExam = medicalAdmissionExam;
                employee.MilitaryDocument = militaryDocument;
                employee.CompleteAdmission(registration!, (DateOnly)dateInitContract!, contractType!);
            }
            catch { };
            

            return employee;
        }
    }
}
