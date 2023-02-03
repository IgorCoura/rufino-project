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
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
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
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
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
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "UpdatedAt" },
                values: new object[] { new Guid("d006cdeb-4120-4c48-a1d0-3a6504c0ccdf"), new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(4941), "description", "TIGRE", new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(4941) });

            migrationBuilder.InsertData(
                table: "Constructions",
                columns: new[] { "Id", "CorporateName", "CreatedAt", "NickName", "UpdatedAt", "Address_City", "Address_Country", "Address_State", "Address_Street", "Address_ZipCode" },
                values: new object[] { new Guid("cad4da64-e4ab-4b4a-8e83-63fc05fefa64"), "Build LTDA", new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(5150), "Build Ltda", new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(5151), "Piracicaba", "Brasil", "Sao Paulo", "Dom Pedro", "99999-000" });

            migrationBuilder.InsertData(
                table: "FunctionsIds",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0e8cd6e5-4ad9-4fe8-b701-548dba923ba8"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1004", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("0f7ab23d-e040-4f78-a981-b68239bb67fe"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1012", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("1c55cb36-d9b6-430e-9c7f-9b1dd8f359cd"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1011", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("2f7c0727-f0ce-42ba-86f2-d2432ea23dad"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1006", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("3aa12411-9e7c-427f-828b-af39f98e7d61"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1010", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("3c02e4ab-f25d-4fec-a8bc-75094e0f5008"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1009", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("3dbed3d8-0ca8-44ed-b584-d6d7c5981f10"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1002", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("78d8138f-7611-48bd-8c19-4f7bde11d4cb"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1005", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("79782c6e-49b9-4772-9555-135439779162"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1013", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("807c1a3b-48d3-4bdb-9b72-f7a3df841d40"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1018", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("88b47f58-331f-465c-a99e-164339cac45d"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1017", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("8b0dc6c4-f111-470c-a0e7-c86d24042d04"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1003", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("9caf5d43-bcb3-480a-855c-95cb334fb567"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1001", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("9ed9f78a-edc4-44ab-9976-5b53be65273d"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1007", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a2d2546f-2382-4a71-a4fb-cad76f0515cc"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1015", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("b2f9f1e8-20e1-444a-8094-64e169009166"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1014", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("e40c3197-9e5d-442a-8ce7-5f156324a35e"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1016", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("e425be12-6bbd-419f-8615-a9cefbd5091b"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "1008", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Materials",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "Unity", "UpdatedAt" },
                values: new object[] { new Guid("4b867637-aa7c-4878-9383-176a34ee3791"), new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(4914), "description", "TUBO DE PVC", "Metro", new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(4916) });

            migrationBuilder.InsertData(
                table: "Providers",
                columns: new[] { "Id", "Cnpj", "CreatedAt", "Description", "Email", "Name", "Phone", "Site", "UpdatedAt", "Address_City", "Address_Country", "Address_State", "Address_Street", "Address_ZipCode" },
                values: new object[] { new Guid("8299c0dc-927d-45de-b2c8-71c38faf9384"), "02.624.999/0001-23", new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(4961), "description", "ponto@email.com", "PONTO DO ENCANADOR", "Phone", "Site.com", new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(4961), "Piracicaba", "Brasil", "Sao Paulo", "Dom Pedro", "99999-000" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Role", "UpdatedAt", "Username" },
                values: new object[] { new Guid("4922766e-d3ba-4d4c-99b0-093d5977d41f"), new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(4656), "admin", new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(4665), "admin" });

            migrationBuilder.InsertData(
                table: "ConstructionAuthUserGroups",
                columns: new[] { "Id", "ConstructionId", "CreatedAt", "Priority", "UpdatedAt" },
                values: new object[] { new Guid("e6389915-3947-46d1-a636-da6f9ad505aa"), new Guid("cad4da64-e4ab-4b4a-8e83-63fc05fefa64"), new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(5270), 0, new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(5271) });

            migrationBuilder.InsertData(
                table: "FunctionIdRole",
                columns: new[] { "FunctionsIdsId", "RolesId" },
                values: new object[,]
                {
                    { new Guid("0e8cd6e5-4ad9-4fe8-b701-548dba923ba8"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("0f7ab23d-e040-4f78-a981-b68239bb67fe"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("1c55cb36-d9b6-430e-9c7f-9b1dd8f359cd"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("2f7c0727-f0ce-42ba-86f2-d2432ea23dad"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("3aa12411-9e7c-427f-828b-af39f98e7d61"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("3c02e4ab-f25d-4fec-a8bc-75094e0f5008"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("3dbed3d8-0ca8-44ed-b584-d6d7c5981f10"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("78d8138f-7611-48bd-8c19-4f7bde11d4cb"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("79782c6e-49b9-4772-9555-135439779162"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("807c1a3b-48d3-4bdb-9b72-f7a3df841d40"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("88b47f58-331f-465c-a99e-164339cac45d"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("8b0dc6c4-f111-470c-a0e7-c86d24042d04"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("9caf5d43-bcb3-480a-855c-95cb334fb567"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("9ed9f78a-edc4-44ab-9976-5b53be65273d"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("a2d2546f-2382-4a71-a4fb-cad76f0515cc"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("b2f9f1e8-20e1-444a-8094-64e169009166"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("e40c3197-9e5d-442a-8ce7-5f156324a35e"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") },
                    { new Guid("e425be12-6bbd-419f-8615-a9cefbd5091b"), new Guid("2dcb40c6-6508-4d88-af05-bbdf96584609") }
                });

            migrationBuilder.InsertData(
                table: "ConstructionUserAuthorizations",
                columns: new[] { "Id", "AuthorizationStatus", "AuthorizationUserGroupId", "Comment", "CreatedAt", "Permissions", "UpdatedAt", "UserId" },
                values: new object[] { new Guid("38db1f77-f9d2-414c-aeeb-35a57ac4cc2e"), 0, new Guid("e6389915-3947-46d1-a636-da6f9ad505aa"), "", new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(5292), 4, new DateTime(2023, 2, 3, 14, 30, 27, 418, DateTimeKind.Local).AddTicks(5293), new Guid("4922766e-d3ba-4d4c-99b0-093d5977d41f") });

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
