using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseAuthUserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
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
                values: new object[] { new Guid("b11c344b-b3f5-4b49-a33f-89d7e945c339"), new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9311), "description", "TIGRE", new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9312) });

            migrationBuilder.InsertData(
                table: "Constructions",
                columns: new[] { "Id", "CorporateName", "CreatedAt", "NickName", "UpdatedAt", "Address_City", "Address_Country", "Address_State", "Address_Street", "Address_ZipCode" },
                values: new object[] { new Guid("cad4da64-e4ab-4b4a-8e83-63fc05fefa64"), "Build LTDA", new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9698), "Build Ltda", new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9700), "Piracicaba", "Brasil", "Sao Paulo", "Dom Pedro", "99999-000" });

            migrationBuilder.InsertData(
                table: "Materials",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "Unity", "UpdatedAt" },
                values: new object[] { new Guid("31f57a85-0dcc-4c0d-ba9c-9e66886c8612"), new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9273), "description", "TUBO DE PVC", "Metro", new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9275) });

            migrationBuilder.InsertData(
                table: "Providers",
                columns: new[] { "Id", "Cnpj", "CreatedAt", "Description", "Email", "Name", "Phone", "Site", "UpdatedAt", "Address_City", "Address_Country", "Address_State", "Address_Street", "Address_ZipCode" },
                values: new object[] { new Guid("8299c0dc-927d-45de-b2c8-71c38faf9384"), "02.624.999/0001-23", new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9412), "description", "ponto@email.com", "PONTO DO ENCANADOR", "Phone", "Site.com", new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9414), "Piracicaba", "Brasil", "Sao Paulo", "Dom Pedro", "99999-000" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Role", "UpdatedAt", "Username" },
                values: new object[] { new Guid("4922766e-d3ba-4d4c-99b0-093d5977d41f"), new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9010), "11", new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9021), "admin" });

            migrationBuilder.InsertData(
                table: "ConstructionAuthUserGroups",
                columns: new[] { "Id", "ConstructionId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("e6389915-3947-46d1-a636-da6f9ad505aa"), new Guid("cad4da64-e4ab-4b4a-8e83-63fc05fefa64"), new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9915), new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9917) });

            migrationBuilder.InsertData(
                table: "ConstructionUserAuthorizations",
                columns: new[] { "Id", "AuthorizationStatus", "AuthorizationUserGroupId", "Comment", "CreatedAt", "Permissions", "UpdatedAt", "UserId" },
                values: new object[] { new Guid("404d1ea8-8853-45b7-b9f3-02888579d5a2"), 0, new Guid("e6389915-3947-46d1-a636-da6f9ad505aa"), "", new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9952), 3, new DateTime(2023, 1, 25, 23, 27, 37, 222, DateTimeKind.Local).AddTicks(9953), new Guid("4922766e-d3ba-4d4c-99b0-093d5977d41f") });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConstructionUserAuthorizations");

            migrationBuilder.DropTable(
                name: "PurchaseDeliveryItems");

            migrationBuilder.DropTable(
                name: "PurchaseUserAuthorizations");

            migrationBuilder.DropTable(
                name: "ConstructionAuthUserGroups");

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
