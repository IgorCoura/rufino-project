using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using PeopleManagement.Infra.Context;
using AddressCompany = PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Address;
using AddressWorkplace = PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Address;
using ContactCompany = PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Contact;
using EmplyeeAddress = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Address;
using FileArchive = PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.File;
using ExtensionArchive = PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Extension;
using EmployeeContact = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Contact;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using System.Data;

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

       
        public static async Task<Role> InsertRole(this PeopleManagementContext context, Guid companyId, CancellationToken cancellationToken = default)
        {
            var departmentId = Guid.NewGuid();
            var departament = Department.Create(departmentId, "Hidraulica", "Hidraulica", companyId);
            await context.Departments.AddAsync(departament, cancellationToken);

            var postionId = Guid.NewGuid();
            var position = Position.Create(postionId, "Encanador", "Encanador", "738298", departmentId, companyId);
            await context.Positions.AddAsync(position, cancellationToken);
            
            var roleId = Guid.NewGuid();
            var role = Role.Create(roleId, "Encanador Senior", "Encanador Com Experiencia", "738298", Remuneration.Create(PaymentUnit.PerHour, Currency.Create(CurrencyType.BRL, "10.55"), "Por Hora"), postionId, companyId);
            await context.Roles.AddAsync(role, cancellationToken);

            return role;
        }

        public static async Task<Workplace> InsertWorkplace(this PeopleManagementContext context, Guid companyId, CancellationToken cancellationToken = default)
        {
            var workplaceId = Guid.NewGuid();
            var workplace = Workplace.Create(workplaceId, "Eleve", AddressWorkplace.Create(
                "14093636",
                "Rua José Otávio de Oliveira",
                "776",
                "",
                "Parque dos Flamboyans",
                "Ribeirão Preto",
                "SP",
                "BRASIL"), companyId);

            await context.Workplaces.AddAsync(workplace, cancellationToken);

            return workplace;
        }

        public static async Task<Employee> InsertEmployeeWithMinimalInfos(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var company = await context.InsertCompany(cancellationToken);
            await context.InsertArchiveCategory(company.Id, cancellationToken);

            var id = Guid.NewGuid();

            var employee = Employee.Create(id, company.Id, "Rosdevaldo Pereira");

            await context.Employees.AddAsync(employee, cancellationToken);

            return employee;
        }

        public static async Task<Employee> InsertEmployeeWithOneDependent(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var company = await context.InsertCompany(cancellationToken);
            await context.InsertArchiveCategory(company.Id, cancellationToken);

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
                        DateOnly.FromDateTime(DateTime.Now.AddYears(-30))
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
            var role = await context.InsertRole(company.Id, cancellationToken);
            var workplace = await context.InsertWorkplace(company.Id, cancellationToken);
            var documentTemplate = await context.InsertDocumentTemplate(company.Id, cancellationToken);
            var requiresDocuments = await context.InsertRequireDocuments(company.Id, role.Id, [documentTemplate.Id], cancellationToken);
            await context.InsertArchiveCategory(company.Id, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            var id = Guid.NewGuid();
            var employee = Employee.Create(id, company.Id, "Rosdevaldo Pereira");
            employee.RoleId = role.Id;
            employee.WorkPlaceId = workplace.Id;

            employee.Address = EmplyeeAddress.Create("69015-756", "Rua 11", "936", "", "Colônia Terra Nova", "Manaus", "Amazonia", "Brasil");
            employee.Contact = EmployeeContact.Create("email@email.com", "(00) 100000001");

            employee.PersonalInfo = PersonalInfo.Create(Deficiency.Create("", []), MaritalStatus.Single, Gender.MALE, Ethinicity.White, EducationLevel.CompleteHigher);
            employee.IdCard = IdCard.Create("216.456.330-12", "Maria Silva", "Marcio Andrade", "Suzano", "São Paulo", "Brasileiro", DateOnly.FromDateTime(DateTime.Now.AddYears(-20)));
            employee.VoteId = VoteId.Create("281662310124");

            employee.MedicalAdmissionExam = MedicalAdmissionExam.Create(DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now.AddDays(365)));

            employee.MilitaryDocument = MilitaryDocument.Create("2312312312", "Rersevista");

            await context.Employees.AddAsync(employee, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            await context.SendRequiresFiles(employee.Id, employee.CompanyId, cancellationToken);

            return employee;
        }

        public static async Task<Employee> InsertEmployeeActive(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var company = await context.InsertCompany(cancellationToken);            
            var role = await context.InsertRole(company.Id, cancellationToken);
            var workplace = await context.InsertWorkplace(company.Id, cancellationToken);
            var documentTemplate = await context.InsertDocumentTemplate(company.Id, cancellationToken);
            var requiresDocuments = await context.InsertRequireDocuments(company.Id, role.Id, [documentTemplate.Id], cancellationToken);
            await context.InsertArchiveCategory(company.Id, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            var id = Guid.NewGuid();
            var employee = Employee.Create(id, company.Id, "Rosdevaldo Pereira");
            employee.RoleId = role.Id;
            employee.WorkPlaceId = workplace.Id;

            employee.Address = EmplyeeAddress.Create("69015-756", "Rua 11", "936", "", "Colônia Terra Nova", "Manaus", "Amazonia", "Brasil");
            employee.Contact = EmployeeContact.Create("email@email.com", "(00) 100000001");

            employee.PersonalInfo = PersonalInfo.Create(Deficiency.Create("", []), MaritalStatus.Single, Gender.MALE, Ethinicity.White, EducationLevel.CompleteHigher);
            employee.IdCard = IdCard.Create("216.456.330-12", "Maria Silva", "Marcio Andrade", "Suzano", "São Paulo", "Brasileiro", DateOnly.FromDateTime(DateTime.Now.AddYears(-25)));
            employee.VoteId = VoteId.Create("281662310124");

            employee.MedicalAdmissionExam = MedicalAdmissionExam.Create(DateOnly.FromDateTime(DateTime.Now).AddDays(-100), DateOnly.FromDateTime(DateTime.Now.AddDays(265)));

            employee.MilitaryDocument = MilitaryDocument.Create("2312312312", "Rersevista");

            var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);

            employee.CompleteAdmission(Guid.NewGuid().ToString()[..14], dateNow, EmploymentContractType.CLT);

            await context.Employees.AddAsync(employee, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await context.SendRequiresFiles(employee.Id, employee.CompanyId, cancellationToken);

            return employee;
        }

        public static async Task<Employee> InsertEmployeeActive(this PeopleManagementContext context, Guid companyId, Guid roleId, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            var employee = Employee.Create(id, companyId, "Rosdevaldo Pereira");
            var workplace = await context.InsertWorkplace(companyId, cancellationToken);
            await context.InsertArchiveCategory(companyId, cancellationToken); 

            employee.RoleId = roleId;
            employee.WorkPlaceId = workplace.Id;

            employee.Address = EmplyeeAddress.Create("69015-756", "Rua 11", "936", "", "Colônia Terra Nova", "Manaus", "Amazonia", "Brasil");
            employee.Contact = EmployeeContact.Create("email@email.com", "(00) 100000001");

            employee.PersonalInfo = PersonalInfo.Create(Deficiency.Create("", []), MaritalStatus.Single, Gender.MALE, Ethinicity.White, EducationLevel.CompleteHigher);
            employee.IdCard = IdCard.Create("216.456.330-12", "Maria Silva", "Marcio Andrade", "Suzano", "São Paulo", "Brasileiro", DateOnly.FromDateTime(DateTime.Now.AddYears(-25)));
            employee.VoteId = VoteId.Create("281662310124");

            employee.MedicalAdmissionExam = MedicalAdmissionExam.Create(DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now.AddDays(365)));

            employee.MilitaryDocument = MilitaryDocument.Create("2312312312", "Rersevista");

            var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);

            employee.CompleteAdmission(Guid.NewGuid().ToString()[..14], dateNow, EmploymentContractType.CLT);

            await context.Employees.AddAsync(employee, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await context.SendRequiresFiles(employee.Id, employee.CompanyId, cancellationToken);
            
            return employee;
        }

        public static async Task<RequireDocuments> InsertRequireDocuments(this PeopleManagementContext context, Guid companyId, 
            Guid roleId, Guid[] documentTemplates, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();

            var requiresSecurityDocuments = RequireDocuments.Create(id, companyId, roleId, AssociationType.Role, "Doc Role Required", 
                "Description Doc Role Required", [ListenEvent.Create(EmployeeEvent.COMPLETE_ADMISSION_EVENT, [Status.Active.Id]), ListenEvent.Create(EmployeeEvent.ROLE_CHANGE_EVENT, [Status.Active.Id])], [.. documentTemplates]);
            await context.RequireDocuments.AddAsync(requiresSecurityDocuments, cancellationToken);
            return requiresSecurityDocuments;
        }

        public static async Task<DocumentTemplate> InsertDocumentTemplate(this PeopleManagementContext context, 
            CancellationToken cancellationToken = default)
        {
            var company = await context.InsertCompany(cancellationToken);
            var documentTemplate = DocumentTemplate.Create(Guid.NewGuid(), "DocumentTemplateName", 
                "Description Document Template", company.Id, TimeSpan.FromDays(365) , TimeSpan.FromHours(8), 
                TemplateFileInfo.Create(Guid.NewGuid().ToString(), "index.html",
                "header.html", "footer.html", RecoverDataType.NR01), []);
            await context.DocumentTemplates.AddAsync(documentTemplate, cancellationToken);
            return documentTemplate;
        }

        public static async Task<DocumentTemplate> InsertDocumentTemplate(this PeopleManagementContext context, Guid companyId,  CancellationToken cancellationToken = default)
        {
            var documentTemplate = DocumentTemplate.Create(
                Guid.NewGuid(),
                "Template Nr01", 
                "Description Template Nr01",
                companyId, 
                TimeSpan.FromDays(365), 
                TimeSpan.FromHours(8),
                TemplateFileInfo.Create(
                    "NR01",
                    "index.html",
                    "header.html",
                    "footer.html",
                    
                    RecoverDataType.NR01
                    ),
                [
                    PlaceSignature.Create(TypeSignature.Signature,1,20.5, 5.3,5.2,5.5),
                    PlaceSignature.Create(TypeSignature.Visa,1,20,15,3,3),
                ]
                );
            await context.DocumentTemplates.AddAsync(documentTemplate, cancellationToken);
            return documentTemplate;
        }


        public static async Task<Document> InsertDocument(this PeopleManagementContext context, Employee employeeActiveWithOutDocuments, DocumentTemplate documentTemplate, RequireDocuments requireDocuments, CancellationToken cancellationToken = default)
        {
            var securityDocument = Document.Create(Guid.NewGuid(), employeeActiveWithOutDocuments.Id, employeeActiveWithOutDocuments.CompanyId, requireDocuments.Id, documentTemplate.Id, documentTemplate.Name.ToString(), documentTemplate.Description.ToString());
            await context.Documents.AddAsync(securityDocument, cancellationToken);

            return securityDocument;
        }

        public static async Task<Document> InsertDocument(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var company = await context.InsertCompany(cancellationToken);
            var role = await context.InsertRole(company.Id, cancellationToken);

            var documentTemplate = await context.InsertDocumentTemplate(company.Id, cancellationToken);
            var requiresDocuments = await context.InsertRequireDocuments(company.Id, role.Id, [documentTemplate.Id], cancellationToken);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id);
            await context.SaveChangesAsync(cancellationToken);

            var securityDocument = Document.Create(Guid.NewGuid(), employee.Id, employee.CompanyId, requiresDocuments.Id, documentTemplate.Id, documentTemplate.Name.ToString(), documentTemplate.Description.ToString());
            await context.Documents.AddAsync(securityDocument, cancellationToken);

            return securityDocument;
        }

        public static Task<DocumentUnit> InsertOneDocumentWithInfoInDocument(this Document document, bool IsAwaitingSignature = false)
        {
            var content = DataToDocument.GetContent();
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            unit.UpdateDetails(DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now.AddDays(365)), "");
            if (IsAwaitingSignature)
                unit.MarkAsAwaitingSignature();
            document.UpdateDocumentUnitDetails(unit.Id, DateOnly.FromDateTime(DateTime.UtcNow), TimeSpan.FromDays(365), content);
            return Task.FromResult(unit);
        }
        public static Task<DocumentUnit> InsertOneDocumentInDocument(this Document document)
        {
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            return Task.FromResult(unit);
        }

        public static async Task<List<ArchiveCategory>> InsertArchiveCategory(this PeopleManagementContext context, Guid companyId, CancellationToken cancellationToken = default)
        {
            List<ArchiveCategory> categorie = [];
            categorie.Add(ArchiveCategory.Create(Guid.NewGuid(),
                "RG", "Cateira de Identidade",
                [EmployeeEvent.CREATED_EVENT], companyId));
            categorie.Add(ArchiveCategory.Create(Guid.NewGuid(), "TITULO DE ELEITOR",
                "Titulo de eleitor comprovando seu cadastro.",
                [EmployeeEvent.CREATED_EVENT], companyId));
            categorie.Add(ArchiveCategory.Create(Guid.NewGuid(), "COMPROVANTE DE ENDEREÇO",
                "Comprovante de endereço do funcionario.", [EmployeeEvent.CREATED_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.NewGuid(), "CONTRATO DE ADMISSAO",
                "Contrato assinado de adimissao do funcionario.",
                [EmployeeEvent.COMPLETE_ADMISSION_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.NewGuid(), "EXAME ADMISSIONAL",
                "Exame admissional do funcionario comprovando sua aptidão para a função.",
                [EmployeeEvent.COMPLETE_ADMISSION_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.NewGuid(), "DOCUMENTO DE IDENTIFICAÇÃO DO FILHO",
                "Documento de identificação do filho do funcionario com CPF.",
                [EmployeeEvent.DEPENDENT_CHILD_CHANGE_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.NewGuid(), "DOCUMENTO MILITAR",
                "Documento de comprovação de alistamento e dispensa dos serviços militares obrigatorios.",
                [EmployeeEvent.MILITAR_DOCUMENT_CHANGE_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.NewGuid(), "DOCUMENTO DE IDENTIFICAÇÃO DA ESPOSA",
                "Documento de identificação da esposa do funcionario.",
                [EmployeeEvent.DEPENDENT_SPOUSE_CHANGE_EVENT], companyId));

            categorie.Add(ArchiveCategory.Create(Guid.NewGuid(), "EXAME DEMISSIONAL",
                "Exame demissional do funcionario comprovando sua aptidão para a demissão.",
                [EmployeeEvent.DEMISSIONAL_EXAM_REQUEST_EVENT], companyId));

            await context.AddRangeAsync(categorie, cancellationToken);

            return categorie;
        }


        public static async Task<Archive> InsertArchive(this PeopleManagementContext context, Guid companyId, Guid categoryId, CancellationToken cancellationToken)
        {
            var archive = Archive.Create(Guid.NewGuid(), categoryId, Guid.NewGuid(), companyId);
            await context.Archives.AddAsync(archive, cancellationToken);
            return archive;
        }

        public static async Task<Archive> InsertArchiveOneFilePending(this PeopleManagementContext context, Guid companyId, Guid categoryId, CancellationToken cancellationToken)
        {
            var archive = Archive.Create(Guid.NewGuid(), categoryId, Guid.NewGuid(), companyId);

            var file = FileArchive.Create("File", "PDF");
            archive.AddFile(file);
   
            await context.Archives.AddAsync(archive, cancellationToken);
            return archive;
        }


        private static async Task SendRequiresFiles(this PeopleManagementContext context, Guid ownerId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var archives = await context.Archives.Where(x => x.OwnerId == ownerId && x.CompanyId == companyId && x.Status == ArchiveStatus.RequiresFile).ToListAsync(cancellationToken);
            archives.ForEach(x => x.AddFile(FileArchive.CreateWithoutVerification(Guid.NewGuid().ToString(), ExtensionArchive.PDF)));
        }

    }
}
