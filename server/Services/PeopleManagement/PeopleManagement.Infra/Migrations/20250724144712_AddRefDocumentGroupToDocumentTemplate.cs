using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddRefDocumentGroupToDocumentTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DocumentGroupId",
                table: "DocumentTemplates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTemplates_CompanyId",
                table: "DocumentTemplates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTemplates_DocumentGroupId",
                table: "DocumentTemplates",
                column: "DocumentGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTemplates_Companies_CompanyId",
                table: "DocumentTemplates",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTemplates_DocumentGroups_DocumentGroupId",
                table: "DocumentTemplates",
                column: "DocumentGroupId",
                principalTable: "DocumentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTemplates_Companies_CompanyId",
                table: "DocumentTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTemplates_DocumentGroups_DocumentGroupId",
                table: "DocumentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_DocumentTemplates_CompanyId",
                table: "DocumentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_DocumentTemplates_DocumentGroupId",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "DocumentGroupId",
                table: "DocumentTemplates");
        }
    }
}
