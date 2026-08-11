using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddReplacesDocumentUnitIdToDocumentUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReplacesDocumentUnitId",
                schema: "people_management",
                table: "DocumentsUnits",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReplacesDocumentUnitId",
                schema: "people_management",
                table: "DocumentsUnits");
        }
    }
}
