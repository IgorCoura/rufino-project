using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsePreviousPeriod",
                schema: "people_management",
                table: "DocumentTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Period_Day",
                schema: "people_management",
                table: "DocumentsUnits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Period_Month",
                schema: "people_management",
                table: "DocumentsUnits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Period_Type",
                schema: "people_management",
                table: "DocumentsUnits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Period_Week",
                schema: "people_management",
                table: "DocumentsUnits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Period_Year",
                schema: "people_management",
                table: "DocumentsUnits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsePreviousPeriod",
                schema: "people_management",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsePreviousPeriod",
                schema: "people_management",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "Period_Day",
                schema: "people_management",
                table: "DocumentsUnits");

            migrationBuilder.DropColumn(
                name: "Period_Month",
                schema: "people_management",
                table: "DocumentsUnits");

            migrationBuilder.DropColumn(
                name: "Period_Type",
                schema: "people_management",
                table: "DocumentsUnits");

            migrationBuilder.DropColumn(
                name: "Period_Week",
                schema: "people_management",
                table: "DocumentsUnits");

            migrationBuilder.DropColumn(
                name: "Period_Year",
                schema: "people_management",
                table: "DocumentsUnits");

            migrationBuilder.DropColumn(
                name: "UsePreviousPeriod",
                schema: "people_management",
                table: "Documents");
        }
    }
}
