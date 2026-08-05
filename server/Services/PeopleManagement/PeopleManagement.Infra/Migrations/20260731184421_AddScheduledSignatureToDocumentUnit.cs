using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledSignatureToDocumentUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ScheduledSignature_DateLimitToSign",
                schema: "people_management",
                table: "DocumentsUnits",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduledSignature_ReminderEveryNDays",
                schema: "people_management",
                table: "DocumentsUnits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ScheduledSignature_SendOn",
                schema: "people_management",
                table: "DocumentsUnits",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledSignature_DateLimitToSign",
                schema: "people_management",
                table: "DocumentsUnits");

            migrationBuilder.DropColumn(
                name: "ScheduledSignature_ReminderEveryNDays",
                schema: "people_management",
                table: "DocumentsUnits");

            migrationBuilder.DropColumn(
                name: "ScheduledSignature_SendOn",
                schema: "people_management",
                table: "DocumentsUnits");
        }
    }
}
