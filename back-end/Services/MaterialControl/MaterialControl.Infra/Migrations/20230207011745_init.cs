using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaterialControl.Infra.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FunctionsIds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionsIds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FunctionIdRole",
                columns: table => new
                {
                    FunctionsIdsId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionIdRole", x => new { x.FunctionsIdsId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_FunctionIdRole_FunctionsIds_FunctionsIdsId",
                        column: x => x.FunctionsIdsId,
                        principalTable: "FunctionsIds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FunctionIdRole_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "FunctionsIds",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new Guid("8b65d6bc-ae25-4d02-85ea-b4fb10953d34"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2999", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new Guid("dcf5a86c-6e7f-459d-89a0-6736bcea53bc"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "FunctionIdRole",
                columns: new[] { "FunctionsIdsId", "RolesId" },
                values: new object[] { new Guid("8b65d6bc-ae25-4d02-85ea-b4fb10953d34"), new Guid("dcf5a86c-6e7f-459d-89a0-6736bcea53bc") });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionIdRole_RolesId",
                table: "FunctionIdRole",
                column: "RolesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FunctionIdRole");

            migrationBuilder.DropTable(
                name: "FunctionsIds");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
