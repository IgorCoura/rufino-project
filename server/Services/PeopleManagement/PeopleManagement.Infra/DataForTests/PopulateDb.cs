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
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using System.ComponentModel.Design;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

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
                    await context.AddRangeIfNotExistsAsync(companies);
                    var departamets = CreateDepartments(companies[0].Id);
                    await context.AddRangeIfNotExistsAsync(departamets);
                    var positions = CreatePositions(departamets.Select(x => x.Id).ToArray(), companies[0].Id);
                    await context.AddRangeIfNotExistsAsync(positions);
                    var roles = CreateRoles(positions.Select(x => x.Id).ToArray(), companies[0].Id);
                    await context.AddRangeIfNotExistsAsync(roles);
                    var workplaces = CreateWorkPlaces(companies[0].Id);
                    await context.AddRangeIfNotExistsAsync(workplaces);
                    var employees = CreateEmployees(roles, workplaces[0].Id, companies[0].Id);
                    await context.AddRangeIfNotExistsAsync(employees);
                    var archivesCategories = CreateArchiveCategories(companies[0].Id);
                    await context.AddRangeIfNotExistsAsync(archivesCategories);
                    var documentTemplates = CreateDocumentTemplate(companies[0].Id);
                    await context.AddRangeIfNotExistsAsync(documentTemplates);
                    var requireDocuments = CreateRequireDocuments(companies[0].Id, roles[0].Id, documentTemplates);
                    await context.AddRangeIfNotExistsAsync(requireDocuments);
                    var documents = CreateDocuments(employees[0].Id, companies[0].Id, requireDocuments, documentTemplates);
                    await context.AddRangeIfNotExistsAsync(documents);
                    await context.SaveChangesWithoutDispatchEventsAsync();
                    
                }
                catch { }
            }
        }

        public static Company[] CreateCompanies()
        {
            var comapnies = new[] {
                Company.Create(
                    Guid.Parse("665cdd0d-392c-47f7-b13c-de9b3be48a94"),
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
                    Guid.Parse("47985f1b-b005-4686-9d1d-8fd25c671928"),
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
                Department.Create(Guid.Parse("6ee46bc9-f952-4092-bc53-37a9929f5c2d"),"Tecnologia", "Departamento de tecnologia.", companyId),
                Department.Create(Guid.Parse("748440fc-9458-498e-8baa-c0b299315ba8"), "Recursos Humanos", "Departamento de recursos humanos.", companyId),
            };
            return departments;
        }

        public static Position[] CreatePositions(Guid[] departamentId, Guid companydId)
        {
            var positions = new[]
            {
                Position.Create(Guid.Parse("91836bca-26f3-48aa-9846-ad826e2392dc"), "Engenheiro de Software", "Resposavel por criar softwares", "235846", departamentId[0], companydId),
                Position.Create(Guid.Parse("55826abb-b8db-4ba6-b946-c5de12d927a6"), "Engenheiro de dados", "Resposavel por analisar dados", "125489", departamentId[0], companydId),
                Position.Create(Guid.Parse("B8CA9103-6EBA-4947-A422-FEA713912F80"), "Analista de RH", "Resposavel por analisar o RH", "348596", departamentId[1], companydId),
                Position.Create(Guid.Parse("A77201C3-2455-4E85-905F-0964DD22B8B1"), "Gerente de RH", "Resposavel pelo RH", "745825", departamentId[1], companydId),
            };
            return positions;
        }

        public static Role[] CreateRoles(Guid[] positionId, Guid companydId)
        {
            var roles = new[]
            {
                Role.Create(Guid.Parse("ef0dee7d-730e-4bc5-a302-bd85f6bc21cb"), "Engenheiro de Software Junior", "Resposavel por criar softwares", "985685" , Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "35"), "Pagamento Eng Junior" ), positionId[0], companydId),
                Role.Create(Guid.Parse("797361a8-23ce-4372-9119-a932b413be84"), "Engenheiro de Software Senior", "Resposavel por criar softwares", "458256", Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "34"), "Pagamento Eng Senior" ), positionId[0], companydId),
                Role.Create(Guid.Parse("F1C821B8-FA47-404A-A03A-D6300F56EE1B"), "Engenheiro de Dados Junior", "Resposavel por criar dados", "456786" , Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "80"), "Pagamento Eng Junior" ), positionId[1], companydId),
                Role.Create(Guid.Parse("33716EF9-7FD1-4A58-B4F2-80EC6686348B"), "Engenheiro de Dados Senior", "Resposavel por criar dados", "564329", Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "78"), "Pagamento Eng Senior" ), positionId[1], companydId),
                Role.Create(Guid.Parse("60D16F67-D30D-4EDD-A4A2-D0D248F75CCF"), "Analista de RH Senior", "Resposavel por administrar RH", "452587" , Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "32"), "Pagamento AS Senior" ), positionId[2], companydId),
                Role.Create(Guid.Parse("2E4F65AF-4912-4078-8C4B-2E30E97D2E60"), "Analista de RH Junior", "Resposavel por auxilair RH", "525856", Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "42"), "Pagamento AS Senior" ), positionId[2], companydId),
                Role.Create(Guid.Parse("7CB25904-65E4-456A-8769-3418570638EC"), "Gerente de RH Senior", "Resposavel por gerenciar RH", "452587" , Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "55"), "Pagamento GE Junior" ), positionId[3], companydId),
                Role.Create(Guid.Parse("260466D0-BD50-4C17-9284-136A6849A52A"), "Gerente de RH Junior", "Resposavel por auxilair a gerenciar RH", "525856", Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "49"), "Pagamento GE Junior" ), positionId[3], companydId),
            };
            return roles;
        }

        public static Workplace[] CreateWorkPlaces(Guid companyId)
        {
            var workplaces = new[]
            {
                Workplace.Create(
                    Guid.Parse("f4b10503-73c3-41ae-aeb1-f8e9880679aa"),
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
                Workplace.Create(
                    Guid.Parse("a1b2c3d4-5678-90ab-cdef-1234567890ab"),
                    "Escritorio Filial 1",
                    WorkPlaceAddress.Create(
                        "11030-210",
                        "Avenida Paulista",
                        "1000",
                        "Sala 101",
                        "Bela Vista",
                        "São Paulo",
                        "São Paulo",
                        "Brasil"),
                    companyId),
                Workplace.Create(
                    Guid.Parse("b2c3d4e5-6789-01ab-cdef-2345678901bc"),
                    "Escritorio Filial 2",
                    WorkPlaceAddress.Create(
                        "20040-020",
                        "Rua da Assembleia",
                        "200",
                        "Andar 15",
                        "Centro",
                        "Rio de Janeiro",
                        "Rio de Janeiro",
                        "Brasil"),
                    companyId),
                Workplace.Create(
                    Guid.Parse("c3d4e5f6-7890-12ab-cdef-3456789012cd"),
                    "Escritorio Filial 3",
                    WorkPlaceAddress.Create(
                        "30140-010",
                        "Avenida Afonso Pena",
                        "1500",
                        "Sala 202",
                        "Centro",
                        "Belo Horizonte",
                        "Minas Gerais",
                        "Brasil"),
                    companyId)
            };

            return workplaces;
        }

        public static Employee[] CreateEmployees(Role[] roles, Guid workplaceId, Guid companyId)
        {
            var employees = new List<Employee>();

            employees.Add(
                FactoryEmployee(
                    Guid.Parse("18439da0-259e-45df-8ba4-197e1833e555"),
                    companyId,
                    "Andrea Clara Valentina Jesus",
                    roles[0].Id,
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
                        Deficiency.Create("Nenhuma Observação", [Disability.Auditory, Disability.Mental, Disability.Rehabilitated]),
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
                    EmploymentContractType.CLT
                    ));

            employees.Add(
                FactoryEmployee(Guid.Parse("caf35a09-3ec0-48ff-9e67-0155151e81e6"), companyId, "Ana Clara", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("1c76bfcc-a6fc-4f8b-ad2c-dda955f63115"), companyId, "Beatriz Andrade", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("4cb4b7f0-bf56-4d70-85fc-af3ad0b70508"), companyId, "Bruno Costa", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("5e9d0a53-66df-4df9-80a8-64cbe0a7171b"), companyId, "Carlos Eduardo", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("c21c2058-63de-4313-86d6-01fe67b83add"), companyId, "Cláudio Bastos", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("dbd43fd9-70d9-4f49-a524-a13d87da1031"), companyId, "Daniela Lima", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("fa8b7242-ad92-4acc-bf5b-8c1f403b55a4"), companyId, "Débora Fonseca", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("46fbcc36-174b-444f-9409-a874c76121ea"), companyId, "Eduardo Paiva", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("28de6ef1-7686-44b2-a5ab-89f159d6fbfc"), companyId, "Elias Nunes", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("c8f1ae81-c596-4e5a-9ca2-2d05409bc38e"), companyId, "Fernanda Souza", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("aa5ca7f1-6869-4ac0-b702-269a0150154b"), companyId, "Gustavo Alves", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("e07df8e3-5ddd-4a23-a957-3fa1740b5fce"), companyId, "Helena Vieira", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("74470f6a-c049-40b6-9008-ed3536e19554"), companyId, "Isabela Fernandes", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("5f1e0a5b-1344-4213-bfa9-94d66bc56976"), companyId, "João Victor", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("71f1b14b-3ca2-413d-871c-c3c3ea07aa9e"), companyId, "Karen Moura", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("080f9595-17be-482d-8ce4-69fe32cb59e9"), companyId, "Luís Felipe", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("b1dd7362-f99f-4221-80da-ab98bc625c85"), companyId, "Mariana Ribeiro", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("a4166f37-f7d2-4fda-a97a-0f7e402b2012"), companyId, "Nicolas Silveira", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("40360549-4b1a-4505-ba58-022da1cfe9f6"), companyId, "Quésia Monteiro", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("6f20950a-65fb-47c7-a29e-348b1fbae835"), companyId, "Ricardo Faria", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("46188819-da63-47c0-adf9-5319192208f7"), companyId, "Rogerio Marques", roles[0].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("acdb2074-278b-43e6-b389-be0d09c1069b"), companyId, "Sara Martins", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("e835f19a-c17b-4c7f-b526-4d9e6ee049cc"), companyId, "Thiago Silva", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("128be466-99c6-4ce1-8836-83e98b572f0f"), companyId, "Ursula Rodrigues", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("d7cf4476-e26e-4198-87f7-e283ab131b61"), companyId, "Vítor Carvalho", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("c919a4a5-5eb5-44fb-9acb-b85af68bfdf2"), companyId, "Wesley Nascimento", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("b259cd41-6c2d-4de7-8cd8-74af123dc8bb"), companyId, "Xênia Teixeira", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("39a588ef-8e8a-40ae-99f7-1ab3574724f1"), companyId, "Yasmin Duarte", roles[1].Id, workplaceId)
                );
            employees.Add(
                FactoryEmployee(Guid.Parse("a5c7f15f-374e-4019-bd15-34c41bb3b56e"), companyId, "Zeca Amaral", roles[1].Id, workplaceId)
                ); 
  

            

            return employees.ToArray();

        }

        

        public static ArchiveCategory[] CreateArchiveCategories(Guid companyId)
        {
            List<ArchiveCategory> categorie = [];
            categorie.Add(ArchiveCategory.Create(Guid.Parse("e3b67736-42ce-41a6-9b1f-742dee98f106"), 
                "RG", "Cateira de Identidade",
                [EmployeeEvent.CREATED_EVENT], companyId));
            categorie.Add(ArchiveCategory.Create(Guid.Parse("2fb2c14f-8a8a-4a27-9803-e356ff7f355e"), "TITULO DE ELEITOR", 
                "Titulo de eleitor comprovando seu cadastro.",
                [EmployeeEvent.CREATED_EVENT], companyId));
            categorie.Add(ArchiveCategory.Create(Guid.Parse("45e26e0c-eda4-43bb-9ed2-d8178a3a9db3"), "COMPROVANTE DE ENDEREÇO", 
                "Comprovante de endereço do funcionario.", [EmployeeEvent.CREATED_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.Parse("cd51b33a-f27e-43e2-8746-38ef9c62f141"), "CONTRATO DE ADMISSAO", 
                "Contrato assinado de adimissao do funcionario.",
                [EmployeeEvent.COMPLETE_ADMISSION_EVENT], companyId));
            categorie.Add(ArchiveCategory.Create(Guid.Parse("e0a7a37b-d205-4bfe-abbf-e9c8bcc4909d"), "EXAME ADMISSIONAL", 
                "Exame admissional do funcionario comprovando sua aptidão para a função.",
                [EmployeeEvent.COMPLETE_ADMISSION_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.Parse("a64d9f71-0e22-4ff8-ba6d-0ba8b616cee5"), "DOCUMENTO DE IDENTIFICAÇÃO DO FILHO", 
                "Documento de identificação do filho do funcionario com CPF.",
                [EmployeeEvent.DEPENDENT_CHILD_CHANGE_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.Parse("2db92b0d-1042-4c93-9d68-d03690635803"), "DOCUMENTO MILITAR", 
                "Documento de comprovação de alistamento e dispensa dos serviços militares obrigatorios.", 
                [EmployeeEvent.MILITAR_DOCUMENT_CHANGE_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.Parse("29295921-60c7-4cba-8268-571780a4452a"), "DOCUMENTO DE IDENTIFICAÇÃO DA ESPOSA", 
                "Documento de identificação da esposa do funcionario.",
                [EmployeeEvent.DEPENDENT_SPOUSE_CHANGE_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.Parse("8c693e2a-a0fa-4198-9058-0961c5eeefff"), "EXAME DEMISSIONAL", 
                "Exame demissional do funcionario comprovando sua aptidão para a demissão.",
                [EmployeeEvent.DEMISSIONAL_EXAM_REQUEST_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.Parse("494aa6a7-5f12-4e61-9ab5-4beee69e358e"), "DOC TESTE", 
                "Descrição Doc Teste.",
                [EmployeeEvent.CREATED_EVENT, EmployeeEvent.COMPLETE_ADMISSION_EVENT, EmployeeEvent.DEPENDENT_CHILD_CHANGE_EVENT], companyId));

            return categorie.ToArray();
        }

        public static DocumentTemplate[] CreateDocumentTemplate( Guid companyId)
        {
            var documents = new[]
            {
                DocumentTemplate.Create(
                Guid.Parse("7609E019-4D6C-4246-B8A0-1BC22C473349"),
                "Template Nr01",
                "Description Template Nr01",
                companyId,
                TimeSpan.FromDays(365),
                TimeSpan.FromHours(8),
                TemplateFileInfo.Create("NR01",
                "index.html",
                "header.html",
                "footer.html",
                
                RecoverDataType.NR01
                ),
                [
                    PlaceSignature.Create(TypeSignature.Signature,1,20.5, 5.3,5.2,5.5),
                    PlaceSignature.Create(TypeSignature.Visa,1,20,15,3,3),
                ]
                ),
                DocumentTemplate.Create(
                Guid.Parse("D857FAD2-1B5F-44A1-8428-80B7E85C25CE"),
                "Template Nr02",
                "Description Template Nr02",
                companyId,
                TimeSpan.FromDays(365),
                TimeSpan.FromHours(8),
                TemplateFileInfo.Create(
                "NR02",
                "index.html",
                "header.html",
                "footer.html",
                RecoverDataType.NR01
                ),
                [
                    PlaceSignature.Create(TypeSignature.Signature,1,20.5, 5.3,5.2,5.5),
                    PlaceSignature.Create(TypeSignature.Visa,1,20,15,3,3),
                ]
                ),
            };
            return documents;
        }

        public static RequireDocuments[] CreateRequireDocuments(Guid companyId, Guid roleId, DocumentTemplate[] documentsTemplates)
        {
            var documentsTemplateIds = documentsTemplates.Select(x => x.Id).ToList();
            var documents = new[]
            {
                RequireDocuments.Create(
                    Guid.Parse("2D77226E-8F9F-483F-82A9-6E536864903C"),
                    companyId,
                    roleId,
                    AssociationType.Role,
                    "Requerimento de Documentos",
                    "Descrição do requerimento de documentos",
                    [
                        ListenEvent.Create(EmployeeEvent.COMPLETE_ADMISSION_EVENT, [Status.Active.Id, Status.Vacation.Id]),
                    ],
                    documentsTemplateIds
                    ),
                RequireDocuments.Create(
                    Guid.Parse("5BC970E9-B7AD-4AD2-8927-3A7C095AE0E8"),
                    companyId,
                    roleId,
                    AssociationType.Role,
                    "Requerimento de Documentos 2",
                    "Descrição do requerimento de documentos 2",
                    [
                        ListenEvent.Create(EmployeeEvent.COMPLETE_ADMISSION_EVENT, [Status.Active.Id, Status.Vacation.Id]),
                        ListenEvent.Create(EmployeeEvent.FINISHED_CONTRACT_EVENT, [Status.Active.Id, Status.Inactive.Id]),
                    ],
                    documentsTemplateIds
                    )
            };

            return documents;
        }

        public static Document[] CreateDocuments(Guid employeeId, Guid companyId, RequireDocuments[] requiredDocuments, DocumentTemplate[] documentsTemplates)
        {
            var documentsTemplateIds = documentsTemplates.Select(x => x.Id).ToList();
            var requiredDocumentsIds = requiredDocuments.Select(x => x.Id).ToList();
            var documents = new[]
            {
                Document.Create(
                Guid.Parse("CE0FB044-E7A6-4ABE-8A4A-EC60F666D18F"),
                employeeId,
                companyId,
                requiredDocumentsIds[0],
                documentsTemplateIds[0],
                "Documento numero 1",
                "Descrição do documento numero 1"
                ),

            Document.Create(
                Guid.Parse("9024DFA3-DC09-4311-8AC3-287CD179A02A"),
                employeeId,
                companyId,
                requiredDocumentsIds[1],
                documentsTemplateIds[1],
                "Documento numero 2",
                "Descrição do documento numero 2"
                ), 
            };

            documents[0].NewDocumentUnit(
                Guid.Parse("9315E288-F25A-43EA-9B0E-8EFC3FF5CAF6")
                );

            documents[0].UpdateDocumentUnitDetails(
                Guid.Parse("9315E288-F25A-43EA-9B0E-8EFC3FF5CAF6"),
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(120)),
                "Conteudo do documento 1"
                );


            documents[0].NewDocumentUnit(
                Guid.Parse("DB55022B-54CD-47F9-A03A-5F5CF8BA6BE4")
                );

            documents[0].UpdateDocumentUnitDetails(
                Guid.Parse("DB55022B-54CD-47F9-A03A-5F5CF8BA6BE4"),
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(130)),
                "Conteudo do documento 2"
                );

            documents[0].InsertUnitWithoutRequireValidation(
                Guid.Parse("DB55022B-54CD-47F9-A03A-5F5CF8BA6BE4"),
                "Name File 2",
                "pdf"
                );

            return documents;

        }

        private static Employee FactoryEmployee(Guid id, Guid companyId, string Name, Guid? roleId = null, Guid? workplaceId = null,
            EmployeeAddress? address = null, EmployeeContact? contact = null, PersonalInfo? personalInfo = null, IdCard? idCard = null,
            VoteId? voteId = null, MedicalAdmissionExam? medicalAdmissionExam = null, MilitaryDocument? militaryDocument = null,
            string? registration = null, DateOnly? dateInitContract = null, EmploymentContractType? contractType = null)
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

        private static async Task AddRangeIfNotExistsAsync<T>(this PeopleManagementContext context, T[] newItems) where T : Entity
        {
            // Get all existing item names in a single query
            var existingNames = await context.Set<T>()
                .Where(i => newItems.Select(n => n.Id).Contains(i.Id))
                .Select(i => i.Id)
                .ToListAsync();

            // Filter out items that already exist
            var itemsToAdd = newItems
                .Where(n => !existingNames.Contains(n.Id)) // Adjust condition as needed
                .ToList();

            // Add the new items to the context if any exist
            if (itemsToAdd.Count > 0)
            {
                await context.Set<T>().AddRangeAsync(itemsToAdd);
                await context.SaveChangesAsync();
            }
        }
    }
}
