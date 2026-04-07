using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueCpfAndRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_IdCard_Cpf",
                schema: "people_management",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Registration",
                schema: "people_management",
                table: "Employees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Employees_IdCard_Cpf",
                schema: "people_management",
                table: "Employees",
                column: "IdCard_Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Registration",
                schema: "people_management",
                table: "Employees",
                column: "Registration",
                unique: true);
        }
    }
}
