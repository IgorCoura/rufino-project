using Bogus;
using Npgsql;
using PeopleManagement.Infra.Context;
using System.Data;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using AssociationTypeNum = PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.AssociationType;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using Bogus.Extensions.Brazil;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using Microsoft.IdentityModel.Tokens;

namespace PeopleManagement.IntegrationTests.Data
{
    public class PopulateDatabaseDirectly
    {
        public class CompanyDAO
        {
            public Guid Id { get; init; }
            public string Name { get; init; }
            public string FantasyName { get; init; }
            public string CNPJ { get; init; }
            public string Email { get; init; }
            public string Phone { get; init; }
            public string ZipCode { get; init; }
            public string Street { get; init; }
            public string Number { get; init; }
            public string Complement { get; init; }
            public string Neighborhood { get; init; }
            public string City { get; init; }
            public string State { get; init; }
            public string Country { get; init; }

            private CompanyDAO(Guid id, string name, string fantasyName, string cNPJ, string email,
                string phone, string zipCode, string street, string number, string complement,
                string neighborhood, string city, string state, string country)
            {
                Id = id;
                Name = name;
                FantasyName = fantasyName;
                CNPJ = cNPJ;
                Email = email;
                Phone = phone;
                ZipCode = zipCode;
                Street = street;
                Number = number;
                Complement = complement;
                Neighborhood = neighborhood;
                City = city;
                State = state;
                Country = country;
            }

            public static CompanyDAO CreateFix(
                Guid? id = null,
                string name = "Lucas e Ryan Informática Ltda",
                string fantasyName = "Lucas e Ryan Informática",
                string cNPJ = "82161379000186",
                string email = "auditoria@lucaseryaninformaticaltda.com.br",
                string phone = "1637222844",
                string zipCode = "14093636",
                string street = "Rua José Otávio de Oliveira",
                string number = "776",
                string complement = "Apto 1",
                string neighborhood = "Parque dos Flamboyans",
                string city = "Ribeirão Preto",
                string state = "SP",
                string country = "BRASIL")
            {
                id ??= Guid.NewGuid();
                return new CompanyDAO((Guid)id, name, fantasyName, cNPJ, email,
                    phone, zipCode, street, number, complement,
                    neighborhood, city, state, country);
            }

            public static CompanyDAO CreateRandom(
              Guid? id = null,
              string name = "",
              string fantasyName = "",
              string cNPJ = "",
              string email = "",
              string phone = "",
              string zipCode = "",
              string street = "",
              string number = "",
              string? complement = null,
              string neighborhood = "",
              string city = "",
              string state = "",
              string country = "")
            {
                var clientFake = new Faker<CompanyDAO>("pt_BR")
                    .RuleFor(x => x.Id, _ => id ?? Guid.NewGuid())
                    .RuleFor(x => x.Name, f => string.IsNullOrEmpty(name) ? f.Company.CompanyName() : name)
                    .RuleFor(x => x.FantasyName, f => string.IsNullOrEmpty(fantasyName) ? f.Company.CompanySuffix() : fantasyName)
                    .RuleFor(x => x.CNPJ, f => string.IsNullOrEmpty(cNPJ) ? f.Company.Cnpj() : cNPJ)
                    .RuleFor(x => x.Email, f => string.IsNullOrEmpty(email) ? f.Internet.Email() : email)
                    .RuleFor(x => x.Phone, f => string.IsNullOrEmpty(phone) ? f.Phone.PhoneNumber() : phone)
                    .RuleFor(x => x.ZipCode, f => string.IsNullOrEmpty(zipCode) ? f.Address.ZipCode() : zipCode)
                    .RuleFor(x => x.Street, f => string.IsNullOrEmpty(street) ? f.Address.StreetName() : street)
                    .RuleFor(x => x.Number, f => string.IsNullOrEmpty(number) ? f.Address.BuildingNumber() : number)
                    .RuleFor(x => x.Complement, f => complement is null ? f.Lorem.Word() : complement)
                    .RuleFor(x => x.Neighborhood, f => string.IsNullOrEmpty(neighborhood) ? f.Address.CitySuffix() : neighborhood)
                    .RuleFor(x => x.City, f => string.IsNullOrEmpty(city) ? f.Address.City() : city)
                    .RuleFor(x => x.State, f => string.IsNullOrEmpty(state) ? f.Address.StateAbbr() : state)
                    .RuleFor(x => x.Country, f => string.IsNullOrEmpty(country) ? f.Address.Country() : country);

                return clientFake.Generate();
            }


            public async Task InsertInDB(PeopleManagementContext context, CancellationToken cancellationToken = default)
            {
                var sql = @"INSERT INTO ""Companies"" (""Id"", ""CorporateName"", ""FantasyName"", ""Cnpj"", ""Contact_Email"", ""Contact_Phone"", ""Address_ZipCode"", ""Address_Street"", ""Address_Number"", ""Address_Complement"", ""Address_Neighborhood"", ""Address_City"", ""Address_State"", ""Address_Country"", ""CreatedAt"", ""UpdatedAt"")  
                VALUES (@Id, @CorporateName, @FantasyName, @Cnpj, @Contact_Email, @Contact_Phone, @Address_ZipCode, @Address_Street, @Address_Number, @Address_Complement, @Address_Neighborhood, @Address_City, @Address_State, @Address_Country, @CreatedAt, @UpdatedAt)";

                var parameters = new[]
                {
                   new NpgsqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = this.Id },
                   new NpgsqlParameter("@CorporateName", SqlDbType.NVarChar) { Value = this.Name },
                   new NpgsqlParameter("@FantasyName", SqlDbType.NVarChar) { Value = this.FantasyName },
                   new NpgsqlParameter("@Cnpj", SqlDbType.NVarChar) { Value = this.CNPJ },
                   new NpgsqlParameter("@Contact_Email", SqlDbType.NVarChar) { Value = this.Email },
                   new NpgsqlParameter("@Contact_Phone", SqlDbType.NVarChar) { Value = this.Phone },
                   new NpgsqlParameter("@Address_ZipCode", SqlDbType.NVarChar) { Value = this.ZipCode },
                   new NpgsqlParameter("@Address_Street", SqlDbType.NVarChar) { Value = this.Street },
                   new NpgsqlParameter("@Address_Number", SqlDbType.NVarChar) { Value = this.Number },
                   new NpgsqlParameter("@Address_Complement", SqlDbType.NVarChar) { Value = (object)this.Complement ?? DBNull.Value },
                   new NpgsqlParameter("@Address_Neighborhood", SqlDbType.NVarChar) { Value = this.Neighborhood },
                   new NpgsqlParameter("@Address_City", SqlDbType.NVarChar) { Value = this.City },
                   new NpgsqlParameter("@Address_State", SqlDbType.NVarChar) { Value = this.State },
                   new NpgsqlParameter("@Address_Country", SqlDbType.NVarChar) { Value = this.Country },
                   new NpgsqlParameter("@CreatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow },
                   new NpgsqlParameter("@UpdatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow }
               };

                await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
            }

        }

        public class RequiredDocumentDAO
        {
            public Guid Id { get; init; }
            public string Name { get; init; }
            public string Description { get; init; }
            public int AssociationType { get; init; }
            public Guid AssociationId { get; init; }
            public Guid CompanyId { get; init; }
            public List<Guid> DocumentsTemplatesIds { get; init; }
            public List<int> ListenEventsIds { get; init; }

            private RequiredDocumentDAO(Guid id, string name, string description, int associationType, Guid associationId, Guid companyId, List<Guid> documentsTemplatesIds, List<int> listenEventsIds)
            {
                Id = id;
                Name = name;
                Description = description;
                AssociationType = associationType;
                AssociationId = associationId;
                CompanyId = companyId;
                DocumentsTemplatesIds = documentsTemplatesIds;
                ListenEventsIds = listenEventsIds;
            }

            public static RequiredDocumentDAO CreateFix(
                Guid companyId,
                Guid associationId,
                AssociationTypeNum associationType,
                Guid? id = null,
                string name = "Documentos Requiridos para o Cargo associado",
                string description = "Decrição Documentos Requiridos para o Cargo associado",
                List<Guid>? documentsTemplatesIds = null,
                List<int>? listenEventsIds = null)
            {
                id ??= Guid.NewGuid();
                documentsTemplatesIds ??= [];
                listenEventsIds ??= [];
                associationType ??= 1;

                return new RequiredDocumentDAO((Guid)id, name, description, associationType.Id, (Guid)associationId, (Guid)companyId, documentsTemplatesIds, listenEventsIds);
            }

            public static RequiredDocumentDAO CreateRandom(
               Guid companyId,
               Guid associationId,
               AssociationTypeNum associationType,
               Guid? id = null,
               string name = "",
               string description = "",
               List<Guid>? documentsTemplatesIds = null,
               List<int>? listenEventsIds = null)
            {
                documentsTemplatesIds ??= [];
                listenEventsIds ??= [];

                var clientFake = new Faker<RequiredDocumentDAO>("pt_BR")
                    .RuleFor(x => x.Id, _ => id ?? Guid.NewGuid())
                    .RuleFor(x => x.Name, f => string.IsNullOrEmpty(name) ? f.Lorem.Sentence() : name)
                    .RuleFor(x => x.Description, f => string.IsNullOrEmpty(description) ? f.Lorem.Paragraph() : description)
                    .RuleFor(x => x.AssociationType, _ => associationType.Id)
                    .RuleFor(x => x.AssociationId, _ => associationId)
                    .RuleFor(x => x.CompanyId, _ => companyId)
                    .RuleFor(x => x.DocumentsTemplatesIds, _ => documentsTemplatesIds)
                    .RuleFor(x => x.ListenEventsIds, _ => listenEventsIds);

                return clientFake.Generate();
            }


            public async Task InsertInDB(PeopleManagementContext context, CancellationToken cancellationToken = default)
            {
                var sql = @"INSERT INTO ""RequiredDocuments"" (""Id"", ""Name"", ""Description"", ""AssociationType"", ""AssociationId"", ""CompanyId"", ""DocumentsTemplatesIds"", ""ListenEventsIds"", ""CreatedAt"", ""UpdatedAt"")  
                       VALUES (@Id, @Name, @Description, @AssociationType, @AssociationId, @CompanyId, @DocumentsTemplatesIds, @ListenEventsIds, @CreatedAt, @UpdatedAt)";

                var parameters = new[]
                {
                   new NpgsqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = this.Id },
                   new NpgsqlParameter("@Name", SqlDbType.NVarChar) { Value = this.Name },
                   new NpgsqlParameter("@Description", SqlDbType.NVarChar) { Value = this.Description },
                   new NpgsqlParameter("@AssociationType", SqlDbType.Int) { Value = this.AssociationType },
                   new NpgsqlParameter("@AssociationId", SqlDbType.UniqueIdentifier) { Value = this.AssociationId },
                   new NpgsqlParameter("@CompanyId", SqlDbType.UniqueIdentifier) { Value = this.CompanyId },
                   new NpgsqlParameter("@DocumentsTemplatesIds", SqlDbType.Structured) { Value = this.DocumentsTemplatesIds },
                   new NpgsqlParameter("@ListenEventsIds", SqlDbType.Text) { Value = string.Join(",", this.ListenEventsIds) },
                   new NpgsqlParameter("@CreatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow },
                   new NpgsqlParameter("@UpdatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow }
                };

                await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
            }
        }

        public class RoleDAO
        {
            public Guid Id { get; init; }
            public string Name { get; init; }
            public string Description { get; init; }
            public string CBO { get; init; }
            public int RemunerationPaymentUnit { get; init; }
            public int RemunerationBaseSalaryType { get; init; }
            public string RemunerationBaseSalaryValue { get; init; }
            public string RemunerationDescription { get; init; }
            public Guid PositionId { get; init; }
            public Guid CompanyId { get; init; }

            private RoleDAO(Guid id, string name, string description, string cbo, int remunerationPaymentUnit, int remunerationBaseSalaryType, string remunerationBaseSalaryValue, string remunerationDescription, Guid positionId, Guid companyId)
            {
                Id = id;
                Name = name;
                Description = description;
                CBO = cbo;
                RemunerationPaymentUnit = remunerationPaymentUnit;
                RemunerationBaseSalaryType = remunerationBaseSalaryType;
                RemunerationBaseSalaryValue = remunerationBaseSalaryValue;
                RemunerationDescription = remunerationDescription;
                PositionId = positionId;
                CompanyId = companyId;
            }

            public static RoleDAO CreateFix(
                Guid positionId,
                Guid companyId,
                Guid? id = null,
                string name = "Default Role Name",
                string description = "Default Role Description",
                string cbo = "123456",
                PaymentUnit? remunerationPaymentUnit = null,
                CurrencyType? remunerationBaseSalaryType = null,
                string remunerationBaseSalaryValue = "0.00",
                string remunerationDescription = "Default Remuneration Description"
                )
            {
                id ??= Guid.NewGuid();
                remunerationPaymentUnit ??= PaymentUnit.PerHour;
                remunerationBaseSalaryType ??= CurrencyType.BRL;

                return new RoleDAO((Guid)id, name, description, cbo, remunerationPaymentUnit.Id, remunerationBaseSalaryType.Id, remunerationBaseSalaryValue, remunerationDescription, (Guid)positionId, (Guid)companyId);
            }

            public static RoleDAO CreateRandom(
                Guid positionId,
                Guid companyId,
                Guid? id = null,
                string name = "",
                string description = "",
                string cbo = "",
                PaymentUnit? remunerationPaymentUnit = null,
                CurrencyType? remunerationBaseSalaryType = null,
                string remunerationBaseSalaryValue = "",
                string remunerationDescription = ""
                )
            {
                var faker = new Faker<RoleDAO>("pt_BR")
                    .RuleFor(x => x.Id, _ => id ?? Guid.NewGuid())
                    .RuleFor(x => x.Name, f => string.IsNullOrEmpty(name) ? f.Name.JobTitle() : name)
                    .RuleFor(x => x.Description, f => string.IsNullOrEmpty(description) ? f.Lorem.Paragraph() : description)
                    .RuleFor(x => x.CBO, f => string.IsNullOrEmpty(cbo) ? f.Random.ReplaceNumbers("######") : cbo)
                    .RuleFor(x => x.RemunerationPaymentUnit, _ => remunerationPaymentUnit ?? 1)
                    .RuleFor(x => x.RemunerationBaseSalaryType, _ => remunerationBaseSalaryType ?? 1)
                    .RuleFor(x => x.RemunerationBaseSalaryValue, f => string.IsNullOrEmpty(remunerationBaseSalaryValue) ? f.Finance.Amount().ToString("F2") : remunerationBaseSalaryValue)
                    .RuleFor(x => x.RemunerationDescription, f => string.IsNullOrEmpty(remunerationDescription) ? f.Lorem.Sentence() : remunerationDescription)
                    .RuleFor(x => x.PositionId, _ => positionId)
                    .RuleFor(x => x.CompanyId, _ => companyId);

                return faker.Generate();
            }

            public async Task InsertInDB(PeopleManagementContext context, CancellationToken cancellationToken = default)
            {
                var sql = @"INSERT INTO ""Roles"" (""Id"", ""Name"", ""Description"", ""CBO"", ""Remuneration_PaymentUnit"", ""Remuneration_BaseSalary_Type"", ""Remuneration_BaseSalary_Value"", ""Remuneration_Description"", ""PositionId"", ""CompanyId"", ""CreatedAt"", ""UpdatedAt"")  
                           VALUES (@Id, @Name, @Description, @CBO, @Remuneration_PaymentUnit, @Remuneration_BaseSalary_Type, @Remuneration_BaseSalary_Value, @Remuneration_Description, @PositionId, @CompanyId, @CreatedAt, @UpdatedAt)";

                var parameters = new[]
                {
                   new NpgsqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = this.Id },
                   new NpgsqlParameter("@Name", SqlDbType.NVarChar) { Value = this.Name },
                   new NpgsqlParameter("@Description", SqlDbType.NVarChar) { Value = this.Description },
                   new NpgsqlParameter("@CBO", SqlDbType.NVarChar) { Value = this.CBO },
                   new NpgsqlParameter("@Remuneration_PaymentUnit", SqlDbType.Int) { Value = this.RemunerationPaymentUnit },
                   new NpgsqlParameter("@Remuneration_BaseSalary_Type", SqlDbType.Int) { Value = this.RemunerationBaseSalaryType },
                   new NpgsqlParameter("@Remuneration_BaseSalary_Value", SqlDbType.NVarChar) { Value = this.RemunerationBaseSalaryValue },
                   new NpgsqlParameter("@Remuneration_Description", SqlDbType.NVarChar) { Value = this.RemunerationDescription },
                   new NpgsqlParameter("@PositionId", SqlDbType.UniqueIdentifier) { Value = this.PositionId },
                   new NpgsqlParameter("@CompanyId", SqlDbType.UniqueIdentifier) { Value = this.CompanyId },
                   new NpgsqlParameter("@CreatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow },
                   new NpgsqlParameter("@UpdatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow }
               };

                await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
            }

     
        }

        public class PositionDAO
        {
            public Guid Id { get; init; }
            public string Name { get; init; }
            public string Description { get; init; }
            public string CBO { get; init; }
            public Guid DepartmentId { get; init; }
            public Guid CompanyId { get; init; }

            private PositionDAO(Guid id, string name, string description, string cbo, Guid departmentId, Guid companyId)
            {
                Id = id;
                Name = name;
                Description = description;
                CBO = cbo;
                DepartmentId = departmentId;
                CompanyId = companyId;
            }

            public static PositionDAO CreateFix(
                Guid departmentId,
                Guid companyId,
                Guid? id = null,
                string name = "Default Position Name",
                string description = "Default Position Description",
                string cbo = "123456"
                )
            {
                id ??= Guid.NewGuid();

                return new PositionDAO((Guid)id, name, description, cbo, (Guid)departmentId, (Guid)companyId);
            }

            public static PositionDAO CreateRandom(
                Guid departmentId,
                Guid companyId,
                Guid? id = null,
                string name = "",
                string description = "",
                string cbo = "")
            {
                var faker = new Faker<PositionDAO>("pt_BR")
                    .RuleFor(x => x.Id, _ => id ?? Guid.NewGuid())
                    .RuleFor(x => x.Name, f => string.IsNullOrEmpty(name) ? f.Name.JobTitle() : name)
                    .RuleFor(x => x.Description, f => string.IsNullOrEmpty(description) ? f.Lorem.Paragraph() : description)
                    .RuleFor(x => x.CBO, f => string.IsNullOrEmpty(cbo) ? f.Random.ReplaceNumbers("######") : cbo)
                    .RuleFor(x => x.DepartmentId, _ => departmentId)
                    .RuleFor(x => x.CompanyId, _ => companyId);

                return faker.Generate();
            }

            public async Task InsertInDB(PeopleManagementContext context, CancellationToken cancellationToken = default)
            {
                var sql = @"INSERT INTO ""Positions"" (""Id"", ""Name"", ""Description"", ""CBO"", ""DepartmentId"", ""CompanyId"", ""CreatedAt"", ""UpdatedAt"")  
                           VALUES (@Id, @Name, @Description, @CBO, @DepartmentId, @CompanyId, @CreatedAt, @UpdatedAt)";

                var parameters = new[]
                {
                   new NpgsqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = this.Id },
                   new NpgsqlParameter("@Name", SqlDbType.NVarChar) { Value = this.Name },
                   new NpgsqlParameter("@Description", SqlDbType.NVarChar) { Value = this.Description },
                   new NpgsqlParameter("@CBO", SqlDbType.NVarChar) { Value = this.CBO },
                   new NpgsqlParameter("@DepartmentId", SqlDbType.UniqueIdentifier) { Value = this.DepartmentId },
                   new NpgsqlParameter("@CompanyId", SqlDbType.UniqueIdentifier) { Value = this.CompanyId },
                   new NpgsqlParameter("@CreatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow },
                   new NpgsqlParameter("@UpdatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow }
               };

                await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
            }
        }

        public class DepartmentDAO
        {
            public Guid Id { get; init; }
            public string Name { get; init; }
            public string Description { get; init; }
            public Guid CompanyId { get; init; }

            private DepartmentDAO(Guid id, string name, string description, Guid companyId)
            {
                Id = id;
                Name = name;
                Description = description;
                CompanyId = companyId;
            }

            public static DepartmentDAO CreateFix(
                Guid companyId,
                Guid? id = null,
                string name = "Default Department Name",
                string description = "Default Department Description"
                )
            {
                id ??= Guid.NewGuid();

                return new DepartmentDAO((Guid)id, name, description, (Guid)companyId);
            }

            public static DepartmentDAO CreateRandom(
                Guid companyId,
                Guid? id = null,
                string name = "",
                string description = "")
            {
                var faker = new Faker<DepartmentDAO>("pt_BR")
                    .RuleFor(x => x.Id, _ => id ?? Guid.NewGuid())
                    .RuleFor(x => x.Name, f => string.IsNullOrEmpty(name) ? f.Commerce.Department() : name)
                    .RuleFor(x => x.Description, f => string.IsNullOrEmpty(description) ? f.Lorem.Paragraph() : description)
                    .RuleFor(x => x.CompanyId, _ => companyId);

                return faker.Generate();
            }

            public async Task InsertInDB(PeopleManagementContext context, CancellationToken cancellationToken = default)
            {
                var sql = @"INSERT INTO ""Departments"" (""Id"", ""Name"", ""Description"", ""CompanyId"", ""CreatedAt"", ""UpdatedAt"")  
                           VALUES (@Id, @Name, @Description, @CompanyId, @CreatedAt, @UpdatedAt)";

                var parameters = new[]
                {
                   new NpgsqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = this.Id },
                   new NpgsqlParameter("@Name", SqlDbType.NVarChar) { Value = this.Name },
                   new NpgsqlParameter("@Description", SqlDbType.NVarChar) { Value = this.Description },
                   new NpgsqlParameter("@CompanyId", SqlDbType.UniqueIdentifier) { Value = this.CompanyId },
                   new NpgsqlParameter("@CreatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow },
                   new NpgsqlParameter("@UpdatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow }
               };

                await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
            }
        }

        public class DocumentTemplateDAO
        {
            public Guid Id { get; init; }
            public string Name { get; init; }
            public string Description { get; init; }
            public Guid CompanyId { get; init; }
            public string? TemplateFileInfo_Directory { get; init; }
            public string? TemplateFileInfo_BodyFileName { get; init; }
            public string? TemplateFileInfo_HeaderFileName { get; init; }
            public string? TemplateFileInfo_FooterFileName { get; init; }
            public int? TemplateFileInfo_RecoverDataType { get; init; }
            public TimeSpan? DocumentValidityDuration { get; init; }
            public TimeSpan? Workload { get; init; }
            public List<PlaceSignatureDAO> PlaceSignatures { get; init; } = new();

            private DocumentTemplateDAO(Guid id, string name, string description, Guid companyId, string? templateFileInfo_Directory,
                string? templateFileInfo_BodyFileName, string? templateFileInfo_HeaderFileName, string? templateFileInfo_FooterFileName,
                int? templateFileInfo_RecoverDataType, TimeSpan? documentValidityDuration, TimeSpan? workload, List<PlaceSignatureDAO> placeSignatures)
            {
                Id = id;
                Name = name;
                Description = description;
                CompanyId = companyId;
                TemplateFileInfo_Directory = templateFileInfo_Directory;
                TemplateFileInfo_BodyFileName = templateFileInfo_BodyFileName;
                TemplateFileInfo_HeaderFileName = templateFileInfo_HeaderFileName;
                TemplateFileInfo_FooterFileName = templateFileInfo_FooterFileName;
                TemplateFileInfo_RecoverDataType = templateFileInfo_RecoverDataType;
                DocumentValidityDuration = documentValidityDuration;
                Workload = workload;
                PlaceSignatures = placeSignatures;
            }

            public static DocumentTemplateDAO CreateFix(
                Guid companyId,
                Guid? id = null,
                string name = "Default Template Name",
                string description = "Default Template Description",
                string? templateFileInfo_Directory = null,
                string? templateFileInfo_BodyFileName = null,
                string? templateFileInfo_HeaderFileName = null,
                string? templateFileInfo_FooterFileName = null,
                int? templateFileInfo_RecoverDataType = null,
                TimeSpan? documentValidityDuration = null,
                TimeSpan? workload = null,
                List<PlaceSignatureDAO>? placeSignatures = null)
            {
                id ??= Guid.NewGuid();
                placeSignatures ??= new List<PlaceSignatureDAO>();

                return new DocumentTemplateDAO((Guid)id, name, description, (Guid)companyId, templateFileInfo_Directory,
                    templateFileInfo_BodyFileName, templateFileInfo_HeaderFileName, templateFileInfo_FooterFileName,
                    templateFileInfo_RecoverDataType, documentValidityDuration, workload, placeSignatures);
            }

            public static DocumentTemplateDAO CreateRandom(
                Guid companyId,
                Guid? id = null,
                string name = "",
                string description = "",
                string? templateFileInfo_Directory = null,
                string? templateFileInfo_BodyFileName = null,
                string? templateFileInfo_HeaderFileName = null,
                string? templateFileInfo_FooterFileName = null,
                int? templateFileInfo_RecoverDataType = null,
                TimeSpan? documentValidityDuration = null,
                TimeSpan? workload = null,
                List<PlaceSignatureDAO>? placeSignatures = null)
            {
                id ??= Guid.NewGuid();
                var faker = new Faker<DocumentTemplateDAO>("pt_BR")
                    .RuleFor(x => x.Id, _ => id)
                    .RuleFor(x => x.Name, f => string.IsNullOrEmpty(name) ? f.Commerce.ProductName() : name)
                    .RuleFor(x => x.Description, f => string.IsNullOrEmpty(description) ? f.Lorem.Paragraph() : description)
                    .RuleFor(x => x.CompanyId, _ => companyId)
                    .RuleFor(x => x.TemplateFileInfo_Directory, f => templateFileInfo_Directory ?? f.System.DirectoryPath())
                    .RuleFor(x => x.TemplateFileInfo_BodyFileName, f => templateFileInfo_BodyFileName ?? f.System.FileName())
                    .RuleFor(x => x.TemplateFileInfo_HeaderFileName, f => templateFileInfo_HeaderFileName ?? f.System.FileName())
                    .RuleFor(x => x.TemplateFileInfo_FooterFileName, f => templateFileInfo_FooterFileName ?? f.System.FileName())
                    .RuleFor(x => x.TemplateFileInfo_RecoverDataType, _ => templateFileInfo_RecoverDataType ?? 1)
                    .RuleFor(x => x.DocumentValidityDuration, _ => documentValidityDuration ?? TimeSpan.FromDays(30))
                    .RuleFor(x => x.Workload, _ => workload ?? TimeSpan.FromHours(8))
                    .RuleFor(x => x.PlaceSignatures, _ => placeSignatures.IsNullOrEmpty() ? PlaceSignatureDAO.CreateListRandom((Guid)id, quant: 3) : placeSignatures);

                return faker.Generate();
            }

            public async Task InsertInDB(PeopleManagementContext context, CancellationToken cancellationToken = default)
            {
                var sql = @"INSERT INTO ""DocumentTemplates"" (""Id"", ""Name"", ""Description"", ""CompanyId"", ""TemplateFileInfo_Directory"", ""TemplateFileInfo_BodyFileName"", ""TemplateFileInfo_HeaderFileName"", ""TemplateFileInfo_FooterFileName"", ""TemplateFileInfo_RecoverDataType"", ""DocumentValidityDuration"", ""Workload"", ""CreatedAt"", ""UpdatedAt"")  
                           VALUES (@Id, @Name, @Description, @CompanyId, @TemplateFileInfo_Directory, @TemplateFileInfo_BodyFileName, @TemplateFileInfo_HeaderFileName, @TemplateFileInfo_FooterFileName, @TemplateFileInfo_RecoverDataType, @DocumentValidityDuration, @Workload, @CreatedAt, @UpdatedAt)";

                var parameters = new[]
                {
                   new NpgsqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = this.Id },
                   new NpgsqlParameter("@Name", SqlDbType.NVarChar) { Value = this.Name },
                   new NpgsqlParameter("@Description", SqlDbType.NVarChar) { Value = this.Description },
                   new NpgsqlParameter("@CompanyId", SqlDbType.UniqueIdentifier) { Value = this.CompanyId },
                   new NpgsqlParameter("@TemplateFileInfo_Directory", SqlDbType.NVarChar) { Value = (object?)this.TemplateFileInfo_Directory ?? DBNull.Value },
                   new NpgsqlParameter("@TemplateFileInfo_BodyFileName", SqlDbType.NVarChar) { Value = (object?)this.TemplateFileInfo_BodyFileName ?? DBNull.Value },
                   new NpgsqlParameter("@TemplateFileInfo_HeaderFileName", SqlDbType.NVarChar) { Value = (object?)this.TemplateFileInfo_HeaderFileName ?? DBNull.Value },
                   new NpgsqlParameter("@TemplateFileInfo_FooterFileName", SqlDbType.NVarChar) { Value = (object?)this.TemplateFileInfo_FooterFileName ?? DBNull.Value },
                   new NpgsqlParameter("@TemplateFileInfo_RecoverDataType", SqlDbType.Int) { Value = (object?)this.TemplateFileInfo_RecoverDataType ?? DBNull.Value },
                   new NpgsqlParameter("@DocumentValidityDuration", SqlDbType.Time) { Value = (object?)this.DocumentValidityDuration ?? DBNull.Value },
                   new NpgsqlParameter("@Workload", SqlDbType.Time) { Value = (object?)this.Workload ?? DBNull.Value },
                   new NpgsqlParameter("@CreatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow },
                   new NpgsqlParameter("@UpdatedAt", SqlDbType.DateTime) { Value = DateTime.UtcNow }
               };

                await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);

                foreach (var placeSignature in PlaceSignatures)
                {
                    await placeSignature.InsertInDB(context, this.Id, cancellationToken);
                }
            }

            public class PlaceSignatureDAO
            {
                public Guid DocumentTemplateId { get; init; }
                public int Id { get; init; }
                public int Type { get; init; }
                public double Page { get; init; }
                public double RelativePositionBotton { get; init; }
                public double RelativePositionLeft { get; init; }
                public double RelativeSizeX { get; init; }
                public double RelativeSizeY { get; init; }

                private PlaceSignatureDAO(Guid documentTemplateId, int id, int type, double page, double relativePositionBotton, double relativePositionLeft, double relativeSizeX, double relativeSizeY)
                {
                    this.DocumentTemplateId = documentTemplateId;
                    Id = id;
                    Type = type;
                    Page = page;
                    RelativePositionBotton = relativePositionBotton;
                    RelativePositionLeft = relativePositionLeft;
                    RelativeSizeX = relativeSizeX;
                    RelativeSizeY = relativeSizeY;
                }

                public static PlaceSignatureDAO CreateFix(
                    Guid documentTemplateId,
                    int id = 0,
                    int type = 1,
                    double page = 1,
                    double relativePositionBotton = 0.1,
                    double relativePositionLeft = 0.1,
                    double relativeSizeX = 0.1,
                    double relativeSizeY = 0.1)
                {
                    return new PlaceSignatureDAO(documentTemplateId, id, type, page, relativePositionBotton, relativePositionLeft, relativeSizeX, relativeSizeY);
                }

                public static PlaceSignatureDAO CreateRandom(
                   Guid documentTemplateId,
                   int? id = null,
                   int? type = null,
                   double? page = null,
                   double? relativePositionBotton = null,
                   double? relativePositionLeft = null,
                   double? relativeSizeX = null,
                   double? relativeSizeY = null)
                {
                    var faker = new Faker<PlaceSignatureDAO>("pt_BR")
                        .RuleFor(x => x.DocumentTemplateId, _ => documentTemplateId)
                        .RuleFor(x => x.Id, f => id ?? f.Random.Int(1, 1000))
                        .RuleFor(x => x.Type, f => type ?? f.Random.Int(1, 5))
                        .RuleFor(x => x.Page, f => page ?? f.Random.Double(1, 10))
                        .RuleFor(x => x.RelativePositionBotton, f => relativePositionBotton ?? f.Random.Double(0.0, 1.0))
                        .RuleFor(x => x.RelativePositionLeft, f => relativePositionLeft ?? f.Random.Double(0.0, 1.0))
                        .RuleFor(x => x.RelativeSizeX, f => relativeSizeX ?? f.Random.Double(0.1, 1.0))
                        .RuleFor(x => x.RelativeSizeY, f => relativeSizeY ?? f.Random.Double(0.1, 1.0));

                    return faker.Generate();
                }

                public static List<PlaceSignatureDAO> CreateListRandom(Guid documentTemplateId, int quant = 1)
                {
                    var list = new List<PlaceSignatureDAO>();
                    for (int i = 0; i < quant; i++)
                    {
                        var placeSignature = CreateRandom(documentTemplateId);
                        list.Add(placeSignature);
                    }
                    return list;
                }


                public async Task InsertInDB(PeopleManagementContext context, Guid documentTemplateId, CancellationToken cancellationToken = default)
                {
                    var sql = @"INSERT INTO ""PlaceSignatures"" (""TemplateFileInfoDocumentTemplateId"", ""Id"", ""Type"", ""Page"", ""RelativePositionBotton"", ""RelativePositionLeft"", ""RelativeSizeX"", ""RelativeSizeY"")  
                               VALUES (@TemplateFileInfoDocumentTemplateId, @Id, @Type, @Page, @RelativePositionBotton, @RelativePositionLeft, @RelativeSizeX, @RelativeSizeY)";

                    var parameters = new[]
                    {
                       new NpgsqlParameter("@TemplateFileInfoDocumentTemplateId", SqlDbType.UniqueIdentifier) { Value = documentTemplateId },
                       new NpgsqlParameter("@Id", SqlDbType.Int) { Value = this.Id },
                       new NpgsqlParameter("@Type", SqlDbType.Int) { Value = this.Type },
                       new NpgsqlParameter("@Page", SqlDbType.Float) { Value = this.Page },
                       new NpgsqlParameter("@RelativePositionBotton", SqlDbType.Float) { Value = this.RelativePositionBotton },
                       new NpgsqlParameter("@RelativePositionLeft", SqlDbType.Float) { Value = this.RelativePositionLeft },
                       new NpgsqlParameter("@RelativeSizeX", SqlDbType.Float) { Value = this.RelativeSizeX },
                       new NpgsqlParameter("@RelativeSizeY", SqlDbType.Float) { Value = this.RelativeSizeY }
                   };

                    await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
                }
            }
        }

    }
}
