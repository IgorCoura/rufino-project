using System;
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
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workplaces", x => x.Id);
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Status = table.Column<string>(type: "text", nullable: false),
                    Sip = table.Column<string>(type: "text", nullable: true),
                    MedicalAdmissionExam_DateExam = table.Column<DateOnly>(type: "date", nullable: true),
                    MedicalAdmissionExam_Validity = table.Column<DateOnly>(type: "date", nullable: true),
                    PersonalInfo_Deficiency_Disabilities = table.Column<string>(type: "text", nullable: true),
                    PersonalInfo_Deficiency_Observation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PersonalInfo_MaritalStatus = table.Column<string>(type: "text", nullable: true),
                    PersonalInfo_Gender = table.Column<string>(type: "text", nullable: true),
                    PersonalInfo_Ethinicity = table.Column<string>(type: "text", nullable: true),
                    PersonalInfo_EducationLevel = table.Column<string>(type: "text", nullable: true),
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Workplaces_WorkPlaceId",
                        column: x => x.WorkPlaceId,
                        principalTable: "Workplaces",
                        principalColumn: "Id");
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
                    Gender = table.Column<string>(type: "text", nullable: false),
                    DependencyType = table.Column<string>(type: "text", nullable: false)
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
                name: "EmploymentContract",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FinalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ContractType = table.Column<string>(type: "text", nullable: false)
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
                name: "IX_Positions_DepartmentId",
                table: "Positions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_PositionId",
                table: "Roles",
                column: "PositionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dependent");

            migrationBuilder.DropTable(
                name: "EmploymentContract");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Workplaces");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
