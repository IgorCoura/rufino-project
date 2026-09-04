using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenant_management");

            migrationBuilder.CreateTable(
                name: "client_requests",
                schema: "tenant_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "tenant_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trade_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    primary_tax_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_phone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    address_zip_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    address_street = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    address_complement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address_neighborhood = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address_city = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address_state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    address_country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    suspension_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_memberships",
                schema: "tenant_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    identity_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provisioning = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_memberships_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "tenant_management",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_products",
                schema: "tenant_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deactivated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_products_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "tenant_management",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_memberships_tenant_email",
                schema: "tenant_management",
                table: "tenant_memberships",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_products_tenant_code",
                schema: "tenant_management",
                table: "tenant_products",
                columns: new[] { "tenant_id", "product_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenants_created_at_id",
                schema: "tenant_management",
                table: "tenants",
                columns: new[] { "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_tenants_primary_tax_id",
                schema: "tenant_management",
                table: "tenants",
                column: "primary_tax_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_requests",
                schema: "tenant_management");

            migrationBuilder.DropTable(
                name: "tenant_memberships",
                schema: "tenant_management");

            migrationBuilder.DropTable(
                name: "tenant_products",
                schema: "tenant_management");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "tenant_management");
        }
    }
}
