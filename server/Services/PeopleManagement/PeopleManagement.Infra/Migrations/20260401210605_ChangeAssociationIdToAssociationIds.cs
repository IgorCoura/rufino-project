using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAssociationIdToAssociationIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequireDocuments_AssociationId",
                schema: "people_management",
                table: "RequireDocuments");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                schema: "people_management",
                table: "RequireDocuments");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "AssociationIds",
                schema: "people_management",
                table: "RequireDocuments",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::uuid[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssociationIds",
                schema: "people_management",
                table: "RequireDocuments");

            migrationBuilder.AddColumn<Guid>(
                name: "AssociationId",
                schema: "people_management",
                table: "RequireDocuments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_RequireDocuments_AssociationId",
                schema: "people_management",
                table: "RequireDocuments",
                column: "AssociationId");
        }
    }
}
