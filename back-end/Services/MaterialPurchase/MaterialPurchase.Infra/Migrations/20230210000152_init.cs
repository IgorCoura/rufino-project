using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MaterialPurchase.Infra.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Constructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorporateName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NickName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AddressStreet = table.Column<string>(name: "Address_Street", type: "character varying(50)", maxLength: 50, nullable: true),
                    AddressCity = table.Column<string>(name: "Address_City", type: "character varying(50)", maxLength: 50, nullable: true),
                    AddressState = table.Column<string>(name: "Address_State", type: "character varying(50)", maxLength: 50, nullable: true),
                    AddressCountry = table.Column<string>(name: "Address_Country", type: "character varying(50)", maxLength: 50, nullable: true),
                    AddressZipCode = table.Column<string>(name: "Address_ZipCode", type: "character varying(16)", maxLength: 16, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Constructions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FunctionsIds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionsIds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unity = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Site = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddressStreet = table.Column<string>(name: "Address_Street", type: "character varying(50)", maxLength: 50, nullable: true),
                    AddressCity = table.Column<string>(name: "Address_City", type: "character varying(50)", maxLength: 50, nullable: true),
                    AddressState = table.Column<string>(name: "Address_State", type: "character varying(50)", maxLength: 50, nullable: true),
                    AddressCountry = table.Column<string>(name: "Address_Country", type: "character varying(50)", maxLength: 50, nullable: true),
                    AddressZipCode = table.Column<string>(name: "Address_ZipCode", type: "character varying(16)", maxLength: 16, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConstructionAuthUserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstructionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstructionAuthUserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstructionAuthUserGroups_Constructions_ConstructionId",
                        column: x => x.ConstructionId,
                        principalTable: "Constructions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstructionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Freight = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LimitDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchases_Constructions_ConstructionId",
                        column: x => x.ConstructionId,
                        principalTable: "Constructions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "ConstructionUserAuthorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationUserGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationStatus = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Permissions = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstructionUserAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstructionUserAuthorizations_ConstructionAuthUserGroups_A~",
                        column: x => x.AuthorizationUserGroupId,
                        principalTable: "ConstructionAuthUserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConstructionUserAuthorizations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemMaterialPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(13,4)", precision: 13, scale: 4, nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemMaterialPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemMaterialPurchases_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemMaterialPurchases_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemMaterialPurchases_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseAuthUserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseAuthUserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseAuthUserGroups_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseDeliveryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialPurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<double>(type: "double precision", precision: 13, scale: 2, nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseDeliveryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseDeliveryItems_ItemMaterialPurchases_MaterialPurchas~",
                        column: x => x.MaterialPurchaseId,
                        principalTable: "ItemMaterialPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseDeliveryItems_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseDeliveryItems_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseUserAuthorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationUserGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationStatus = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Permissions = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseUserAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseUserAuthorizations_PurchaseAuthUserGroups_Authoriza~",
                        column: x => x.AuthorizationUserGroupId,
                        principalTable: "PurchaseAuthUserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseUserAuthorizations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Brands",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new Guid("2bc27280-cf98-4116-ad6c-d7cf5179f41e"), new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(948), "TIGRE", new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(948) });

            migrationBuilder.InsertData(
                table: "Constructions",
                columns: new[] { "Id", "CorporateName", "CreatedAt", "NickName", "UpdatedAt", "Address_City", "Address_Country", "Address_State", "Address_Street", "Address_ZipCode" },
                values: new object[] { new Guid("cad4da64-e4ab-4b4a-8e83-63fc05fefa64"), "Build LTDA", new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(1141), "Build Ltda", new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(1142), "Piracicaba", "Brasil", "Sao Paulo", "Dom Pedro", "99999-000" });

            migrationBuilder.InsertData(
                table: "FunctionsIds",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0c3054ef-dc4a-428d-9f54-cfb3846b142a"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1002", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("1499bfea-9baf-45be-b76c-b5f3a65b28c9"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1013", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("1a425bc2-246c-4cc5-8041-fec431bea70a"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1012", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("241a5ef5-e5aa-4e36-8d2e-6461dcd56298"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1009", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("2aa5ab7e-a149-4e37-a1ce-a8803ead44db"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1016", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("30fff483-1d53-46bb-b5b2-f3f2f64b935a"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1010", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("3be84b90-5117-4f58-9392-0a9e7ecd5c0b"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1007", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("3cb2b2cc-7731-404d-9b64-c19e4f9d4c72"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1018", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("45b77ac4-436e-4a46-a457-d8e5c1ce9d47"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1014", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("4a78cace-0c5a-40d0-9605-3d713b6121aa"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1005", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("520cea04-f214-4d40-8d62-d3f5c058e869"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1011", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("55b7457c-0e2a-4715-b86b-92cbd6a52546"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1004", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("641442a0-db41-415a-862a-32c01c200163"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1001", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("9097b32e-783c-4309-9c23-f6c08252a489"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1008", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("b1f86d9c-591a-47d3-885d-0ff4c4c3a307"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1003", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("b41af008-1729-4edb-95a3-f46157d8420e"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1006", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("c37b998d-d73e-40bb-987d-afea1616d45b"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1017", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("e5b34b70-30f7-458b-a2f0-ef2e6c3bd011"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1015", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Materials",
                columns: new[] { "Id", "CreatedAt", "Name", "Unity", "UpdatedAt" },
                values: new object[] { new Guid("c855f3cb-6666-4fe2-bf79-a1d8696ef951"), new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(734), "TUBO DE PVC", "Metro", new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(735) });

            migrationBuilder.InsertData(
                table: "Providers",
                columns: new[] { "Id", "Cnpj", "CreatedAt", "Description", "Email", "Name", "Phone", "Site", "UpdatedAt", "Address_City", "Address_Country", "Address_State", "Address_Street", "Address_ZipCode" },
                values: new object[] { new Guid("8299c0dc-927d-45de-b2c8-71c38faf9384"), "02.624.999/0001-23", new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(965), "description", "ponto@email.com", "PONTO DO ENCANADOR", "Phone", "Site.com", new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(965), "Piracicaba", "Brasil", "Sao Paulo", "Dom Pedro", "99999-000" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Role", "UpdatedAt", "Username" },
                values: new object[] { new Guid("4922766e-d3ba-4d4c-99b0-093d5977d41f"), new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(438), "admin", new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(446), "admin" });

            migrationBuilder.InsertData(
                table: "ConstructionAuthUserGroups",
                columns: new[] { "Id", "ConstructionId", "CreatedAt", "Priority", "UpdatedAt" },
                values: new object[] { new Guid("e6389915-3947-46d1-a636-da6f9ad505aa"), new Guid("cad4da64-e4ab-4b4a-8e83-63fc05fefa64"), new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(1541), 0, new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(1542) });

            migrationBuilder.InsertData(
                table: "FunctionIdRole",
                columns: new[] { "FunctionsIdsId", "RolesId" },
                values: new object[,]
                {
                    { new Guid("0c3054ef-dc4a-428d-9f54-cfb3846b142a"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("1499bfea-9baf-45be-b76c-b5f3a65b28c9"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("1a425bc2-246c-4cc5-8041-fec431bea70a"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("241a5ef5-e5aa-4e36-8d2e-6461dcd56298"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("2aa5ab7e-a149-4e37-a1ce-a8803ead44db"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("30fff483-1d53-46bb-b5b2-f3f2f64b935a"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("3be84b90-5117-4f58-9392-0a9e7ecd5c0b"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("3cb2b2cc-7731-404d-9b64-c19e4f9d4c72"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("45b77ac4-436e-4a46-a457-d8e5c1ce9d47"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("4a78cace-0c5a-40d0-9605-3d713b6121aa"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("520cea04-f214-4d40-8d62-d3f5c058e869"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("55b7457c-0e2a-4715-b86b-92cbd6a52546"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("641442a0-db41-415a-862a-32c01c200163"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("9097b32e-783c-4309-9c23-f6c08252a489"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("b1f86d9c-591a-47d3-885d-0ff4c4c3a307"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("b41af008-1729-4edb-95a3-f46157d8420e"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("c37b998d-d73e-40bb-987d-afea1616d45b"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") },
                    { new Guid("e5b34b70-30f7-458b-a2f0-ef2e6c3bd011"), new Guid("ae524c8e-27ef-4f62-8d6a-5df0297fb6c4") }
                });

            migrationBuilder.InsertData(
                table: "ConstructionUserAuthorizations",
                columns: new[] { "Id", "AuthorizationStatus", "AuthorizationUserGroupId", "Comment", "CreatedAt", "Permissions", "UpdatedAt", "UserId" },
                values: new object[] { new Guid("1b505080-5f17-4b2a-84b1-5f43baba35c4"), 0, new Guid("e6389915-3947-46d1-a636-da6f9ad505aa"), "", new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(1558), 4, new DateTime(2023, 2, 9, 21, 1, 52, 6, DateTimeKind.Local).AddTicks(1559), new Guid("4922766e-d3ba-4d4c-99b0-093d5977d41f") });

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionAuthUserGroups_ConstructionId",
                table: "ConstructionAuthUserGroups",
                column: "ConstructionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionUserAuthorizations_AuthorizationUserGroupId",
                table: "ConstructionUserAuthorizations",
                column: "AuthorizationUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionUserAuthorizations_UserId",
                table: "ConstructionUserAuthorizations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionIdRole_RolesId",
                table: "FunctionIdRole",
                column: "RolesId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionsIds_Name",
                table: "FunctionsIds",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemMaterialPurchases_BrandId",
                table: "ItemMaterialPurchases",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMaterialPurchases_MaterialId",
                table: "ItemMaterialPurchases",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMaterialPurchases_PurchaseId",
                table: "ItemMaterialPurchases",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseAuthUserGroups_PurchaseId",
                table: "PurchaseAuthUserGroups",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseDeliveryItems_MaterialPurchaseId",
                table: "PurchaseDeliveryItems",
                column: "MaterialPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseDeliveryItems_PurchaseId",
                table: "PurchaseDeliveryItems",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseDeliveryItems_ReceiverId",
                table: "PurchaseDeliveryItems",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ConstructionId",
                table: "Purchases",
                column: "ConstructionId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ProviderId",
                table: "Purchases",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseUserAuthorizations_AuthorizationUserGroupId",
                table: "PurchaseUserAuthorizations",
                column: "AuthorizationUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseUserAuthorizations_UserId",
                table: "PurchaseUserAuthorizations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConstructionUserAuthorizations");

            migrationBuilder.DropTable(
                name: "FunctionIdRole");

            migrationBuilder.DropTable(
                name: "PurchaseDeliveryItems");

            migrationBuilder.DropTable(
                name: "PurchaseUserAuthorizations");

            migrationBuilder.DropTable(
                name: "ConstructionAuthUserGroups");

            migrationBuilder.DropTable(
                name: "FunctionsIds");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ItemMaterialPurchases");

            migrationBuilder.DropTable(
                name: "PurchaseAuthUserGroups");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "Constructions");

            migrationBuilder.DropTable(
                name: "Providers");
        }
    }
}
