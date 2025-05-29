using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorporateName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FantasyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    Contact_Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Contact_Phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Address_ZipCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Address_Street = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address_Number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Address_Complement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address_Neighborhood = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address_City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address_State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address_Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateFileInfo_Directory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TemplateFileInfo_BodyFileName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TemplateFileInfo_HeaderFileName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TemplateFileInfo_FooterFileName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TemplateFileInfo_RecoversDataType = table.Column<string>(type: "text", nullable: true),
                    DocumentValidityDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Workload = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ListenEventsIds = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveCategories_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequireDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AssociationType = table.Column<int>(type: "integer", nullable: false),
                    AssociationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentsTemplatesIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequireDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequireDocuments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Workplaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address_ZipCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Address_Street = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address_Number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Address_Complement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address_Neighborhood = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address_City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address_State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address_Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workplaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workplaces_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlaceSignature",
                columns: table => new
                {
                    DocumentTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Page = table.Column<double>(type: "double precision", nullable: false),
                    RelativePositionBotton = table.Column<double>(type: "double precision", nullable: false),
                    RelativePositionLeft = table.Column<double>(type: "double precision", nullable: false),
                    RelativeSizeX = table.Column<double>(type: "double precision", nullable: false),
                    RelativeSizeY = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceSignature", x => new { x.DocumentTemplateId, x.Id });
                    table.ForeignKey(
                        name: "FK_PlaceSignature_DocumentTemplates_DocumentTemplateId",
                        column: x => x.DocumentTemplateId,
                        principalTable: "DocumentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Archives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Archives_ArchiveCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ArchiveCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Archives_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CBO = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Positions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTemplateRequireDocuments",
                columns: table => new
                {
                    DocumentTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequireDocumentsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTemplateRequireDocuments", x => new { x.DocumentTemplateId, x.RequireDocumentsId });
                    table.ForeignKey(
                        name: "FK_DocumentTemplateRequireDocuments_DocumentTemplates_Document~",
                        column: x => x.DocumentTemplateId,
                        principalTable: "DocumentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentTemplateRequireDocuments_RequireDocuments_RequireDo~",
                        column: x => x.RequireDocumentsId,
                        principalTable: "RequireDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListenEvent",
                columns: table => new
                {
                    RequireDocumentsId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListenEvent", x => new { x.RequireDocumentsId, x.Id });
                    table.ForeignKey(
                        name: "FK_ListenEvent_RequireDocuments_RequireDocumentsId",
                        column: x => x.RequireDocumentsId,
                        principalTable: "RequireDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "File",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Extension = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InsertAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchiveId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_File", x => x.Id);
                    table.ForeignKey(
                        name: "FK_File_Archives_ArchiveId",
                        column: x => x.ArchiveId,
                        principalTable: "Archives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CBO = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Remuneration_PaymentUnit = table.Column<int>(type: "integer", nullable: false),
                    Remuneration_BaseSalary_Type = table.Column<int>(type: "integer", nullable: false),
                    Remuneration_BaseSalary_Value = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    Remuneration_Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Roles_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Registration = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkPlaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Address_ZipCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Address_Street = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address_Number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Address_Complement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address_Neighborhood = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address_City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address_State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address_Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Contact_Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Contact_CellPhone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MedicalAdmissionExam_DateExam = table.Column<DateOnly>(type: "date", nullable: true),
                    MedicalAdmissionExam_Validity = table.Column<DateOnly>(type: "date", nullable: true),
                    PersonalInfo_Deficiency_Disabilities = table.Column<string>(type: "text", nullable: true),
                    PersonalInfo_Deficiency_Observation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PersonalInfo_MaritalStatus = table.Column<int>(type: "integer", nullable: true),
                    PersonalInfo_Gender = table.Column<int>(type: "integer", nullable: true),
                    PersonalInfo_Ethinicity = table.Column<int>(type: "integer", nullable: true),
                    PersonalInfo_EducationLevel = table.Column<int>(type: "integer", nullable: true),
                    IdCard_Cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    IdCard_MotherName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IdCard_FatherName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IdCard_BirthCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IdCard_BirthState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IdCard_Nacionality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IdCard_DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    VoteId = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    MilitaryDocument_Number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MilitaryDocument_Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Workplaces_WorkPlaceId",
                        column: x => x.WorkPlaceId,
                        principalTable: "Workplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dependent",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdCard_Cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    IdCard_MotherName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdCard_FatherName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdCard_BirthCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdCard_BirthState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdCard_Nacionality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdCard_DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    DependencyType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependent", x => new { x.EmployeeId, x.Id });
                    table.ForeignKey(
                        name: "FK_Dependent_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DocumentTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_DocumentTemplates_DocumentTemplateId",
                        column: x => x.DocumentTemplateId,
                        principalTable: "DocumentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_RequireDocuments_RequiredDocumentId",
                        column: x => x.RequiredDocumentId,
                        principalTable: "RequireDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentContract",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FinalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentContract", x => new { x.EmployeeId, x.Id });
                    table.ForeignKey(
                        name: "FK_EmploymentContract_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentsUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Validity = table.Column<DateOnly>(type: "date", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Extension = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentsUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentsUnits_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveCategories_CompanyId",
                table: "ArchiveCategories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Archives_CategoryId",
                table: "Archives",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Archives_CompanyId",
                table: "Archives",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Archives_OwnerId",
                table: "Archives",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CompanyId",
                table: "Departments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CompanyId",
                table: "Documents",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentTemplateId",
                table: "Documents",
                column: "DocumentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_EmployeeId",
                table: "Documents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RequiredDocumentId",
                table: "Documents",
                column: "RequiredDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsUnits_DocumentId",
                table: "DocumentsUnits",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTemplateRequireDocuments_RequireDocumentsId",
                table: "DocumentTemplateRequireDocuments",
                column: "RequireDocumentsId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId",
                table: "Employees",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Registration",
                table: "Employees",
                column: "Registration",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_RoleId",
                table: "Employees",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_WorkPlaceId",
                table: "Employees",
                column: "WorkPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_File_ArchiveId",
                table: "File",
                column: "ArchiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_CompanyId",
                table: "Positions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_DepartmentId",
                table: "Positions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RequireDocuments_AssociationId",
                table: "RequireDocuments",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_RequireDocuments_CompanyId",
                table: "RequireDocuments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CompanyId",
                table: "Roles",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_PositionId",
                table: "Roles",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Workplaces_CompanyId",
                table: "Workplaces",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientRequests");

            migrationBuilder.DropTable(
                name: "Dependent");

            migrationBuilder.DropTable(
                name: "DocumentsUnits");

            migrationBuilder.DropTable(
                name: "DocumentTemplateRequireDocuments");

            migrationBuilder.DropTable(
                name: "EmploymentContract");

            migrationBuilder.DropTable(
                name: "File");

            migrationBuilder.DropTable(
                name: "ListenEvent");

            migrationBuilder.DropTable(
                name: "PlaceSignature");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Archives");

            migrationBuilder.DropTable(
                name: "DocumentTemplates");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "RequireDocuments");

            migrationBuilder.DropTable(
                name: "ArchiveCategories");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Workplaces");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
