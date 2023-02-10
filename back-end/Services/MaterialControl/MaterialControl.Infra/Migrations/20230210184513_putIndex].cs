using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MaterialControl.Infra.Migrations
{
    /// <inheritdoc />
    public partial class putIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("0ac8f35e-9a8a-4eda-819c-f8db3a90eb34"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("196537c1-7d40-438d-90f3-e355ab0f927e"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("5481ecf7-062c-497e-bb57-0c4c7d033dba"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("5d585d78-945b-41bb-9d92-c98a06b31d45"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("63845308-fea9-4e75-a3ee-e6690ef0f690"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("64cae9f5-fced-4c5e-be14-2c97eefe5ce4"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("66c313a8-bcf0-424d-b71e-8118132f8d2a"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("78add384-59a1-4df0-b862-a04206825f3f"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("7e3bbe14-7cdf-4d95-a58b-f77b196f3e98"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("a5773b18-d2d9-4fe9-a5ac-23d9aa9a8a6a"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("a6ce5a4c-5299-427d-a98f-d8d1d335f05d"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("cc038d6b-3739-4980-896c-f7af9c5275c8"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("d9117560-ce79-4e1a-aa42-15af2afbbe89"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("eb725059-2702-4495-a1da-2ffa205e8cc5"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("ede7cad4-ac6e-4c3d-8ee5-a121836c3d84"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("fbb3fddb-e1b9-4bab-9dc0-4c46e4a1fe14"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") });

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("0ac8f35e-9a8a-4eda-819c-f8db3a90eb34"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("196537c1-7d40-438d-90f3-e355ab0f927e"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("5481ecf7-062c-497e-bb57-0c4c7d033dba"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("5d585d78-945b-41bb-9d92-c98a06b31d45"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("63845308-fea9-4e75-a3ee-e6690ef0f690"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("64cae9f5-fced-4c5e-be14-2c97eefe5ce4"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("66c313a8-bcf0-424d-b71e-8118132f8d2a"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("78add384-59a1-4df0-b862-a04206825f3f"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("7e3bbe14-7cdf-4d95-a58b-f77b196f3e98"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("a5773b18-d2d9-4fe9-a5ac-23d9aa9a8a6a"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("a6ce5a4c-5299-427d-a98f-d8d1d335f05d"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("cc038d6b-3739-4980-896c-f7af9c5275c8"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("d9117560-ce79-4e1a-aa42-15af2afbbe89"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("eb725059-2702-4495-a1da-2ffa205e8cc5"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("ede7cad4-ac6e-4c3d-8ee5-a121836c3d84"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("fbb3fddb-e1b9-4bab-9dc0-4c46e4a1fe14"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa"));

            migrationBuilder.InsertData(
                table: "FunctionsIds",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("14667529-7f21-49cf-afab-5b8c695355c5"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2002", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("1d3f1215-976a-4689-9fcd-695971e0a431"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2015", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("21438f13-527f-4fbb-9960-4dd697df6550"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2008", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("3d86a26d-0897-487d-8e59-787716504e22"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2005", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("7140fba0-486c-4215-814d-576e15c055b3"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2003", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("7328fc29-e3f9-4835-b77d-6f4017dc4d03"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2013", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("8316564c-3c55-48e1-9a47-9747fbe1daa4"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2010", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("8379b437-f1c4-4834-87ea-abc10917c5db"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2009", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("8c42685d-918b-40e5-8ff7-3aa750ed8948"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2014", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("9a7be459-e818-4984-adde-19ef06b113a8"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2012", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a3e1214e-f446-4d67-aaaf-789e155076e2"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2001", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("aa11d20d-ab81-4cc4-a939-d2d48ab48767"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2007", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("aae9049c-9005-416d-9cd7-fc59892659c3"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2011", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("c9e4d021-40b8-4ea5-88ba-0d885ece29ee"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2999", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("e98ccd7d-08b7-49c1-9987-ee2274a2b1f1"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2006", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("fbe9e547-fb6f-4a21-87d3-3d782ba26e6c"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2004", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new Guid("d692a353-edd4-4078-b412-64c31f716fc6"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "FunctionIdRole",
                columns: new[] { "FunctionsIdsId", "RolesId" },
                values: new object[,]
                {
                    { new Guid("14667529-7f21-49cf-afab-5b8c695355c5"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("1d3f1215-976a-4689-9fcd-695971e0a431"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("21438f13-527f-4fbb-9960-4dd697df6550"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("3d86a26d-0897-487d-8e59-787716504e22"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("7140fba0-486c-4215-814d-576e15c055b3"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("7328fc29-e3f9-4835-b77d-6f4017dc4d03"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("8316564c-3c55-48e1-9a47-9747fbe1daa4"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("8379b437-f1c4-4834-87ea-abc10917c5db"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("8c42685d-918b-40e5-8ff7-3aa750ed8948"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("9a7be459-e818-4984-adde-19ef06b113a8"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("a3e1214e-f446-4d67-aaaf-789e155076e2"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("aa11d20d-ab81-4cc4-a939-d2d48ab48767"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("aae9049c-9005-416d-9cd7-fc59892659c3"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("c9e4d021-40b8-4ea5-88ba-0d885ece29ee"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("e98ccd7d-08b7-49c1-9987-ee2274a2b1f1"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") },
                    { new Guid("fbe9e547-fb6f-4a21-87d3-3d782ba26e6c"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Unities_Name",
                table: "Unities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Name",
                table: "Materials",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Unities_Name",
                table: "Unities");

            migrationBuilder.DropIndex(
                name: "IX_Materials_Name",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Name",
                table: "Brands");

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("14667529-7f21-49cf-afab-5b8c695355c5"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("1d3f1215-976a-4689-9fcd-695971e0a431"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("21438f13-527f-4fbb-9960-4dd697df6550"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("3d86a26d-0897-487d-8e59-787716504e22"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("7140fba0-486c-4215-814d-576e15c055b3"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("7328fc29-e3f9-4835-b77d-6f4017dc4d03"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("8316564c-3c55-48e1-9a47-9747fbe1daa4"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("8379b437-f1c4-4834-87ea-abc10917c5db"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("8c42685d-918b-40e5-8ff7-3aa750ed8948"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("9a7be459-e818-4984-adde-19ef06b113a8"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("a3e1214e-f446-4d67-aaaf-789e155076e2"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("aa11d20d-ab81-4cc4-a939-d2d48ab48767"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("aae9049c-9005-416d-9cd7-fc59892659c3"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("c9e4d021-40b8-4ea5-88ba-0d885ece29ee"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("e98ccd7d-08b7-49c1-9987-ee2274a2b1f1"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("fbe9e547-fb6f-4a21-87d3-3d782ba26e6c"), new Guid("d692a353-edd4-4078-b412-64c31f716fc6") });

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("14667529-7f21-49cf-afab-5b8c695355c5"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("1d3f1215-976a-4689-9fcd-695971e0a431"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("21438f13-527f-4fbb-9960-4dd697df6550"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("3d86a26d-0897-487d-8e59-787716504e22"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("7140fba0-486c-4215-814d-576e15c055b3"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("7328fc29-e3f9-4835-b77d-6f4017dc4d03"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("8316564c-3c55-48e1-9a47-9747fbe1daa4"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("8379b437-f1c4-4834-87ea-abc10917c5db"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("8c42685d-918b-40e5-8ff7-3aa750ed8948"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("9a7be459-e818-4984-adde-19ef06b113a8"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("a3e1214e-f446-4d67-aaaf-789e155076e2"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("aa11d20d-ab81-4cc4-a939-d2d48ab48767"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("aae9049c-9005-416d-9cd7-fc59892659c3"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("c9e4d021-40b8-4ea5-88ba-0d885ece29ee"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("e98ccd7d-08b7-49c1-9987-ee2274a2b1f1"));

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("fbe9e547-fb6f-4a21-87d3-3d782ba26e6c"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("d692a353-edd4-4078-b412-64c31f716fc6"));

            migrationBuilder.InsertData(
                table: "FunctionsIds",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0ac8f35e-9a8a-4eda-819c-f8db3a90eb34"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2011", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("196537c1-7d40-438d-90f3-e355ab0f927e"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2012", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("5481ecf7-062c-497e-bb57-0c4c7d033dba"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2002", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("5d585d78-945b-41bb-9d92-c98a06b31d45"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2001", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("63845308-fea9-4e75-a3ee-e6690ef0f690"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2008", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("64cae9f5-fced-4c5e-be14-2c97eefe5ce4"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2999", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("66c313a8-bcf0-424d-b71e-8118132f8d2a"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2014", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("78add384-59a1-4df0-b862-a04206825f3f"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2006", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("7e3bbe14-7cdf-4d95-a58b-f77b196f3e98"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2010", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a5773b18-d2d9-4fe9-a5ac-23d9aa9a8a6a"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2005", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a6ce5a4c-5299-427d-a98f-d8d1d335f05d"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2003", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("cc038d6b-3739-4980-896c-f7af9c5275c8"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2004", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("d9117560-ce79-4e1a-aa42-15af2afbbe89"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2009", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("eb725059-2702-4495-a1da-2ffa205e8cc5"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2007", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("ede7cad4-ac6e-4c3d-8ee5-a121836c3d84"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2015", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("fbb3fddb-e1b9-4bab-9dc0-4c46e4a1fe14"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2013", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "FunctionIdRole",
                columns: new[] { "FunctionsIdsId", "RolesId" },
                values: new object[,]
                {
                    { new Guid("0ac8f35e-9a8a-4eda-819c-f8db3a90eb34"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("196537c1-7d40-438d-90f3-e355ab0f927e"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("5481ecf7-062c-497e-bb57-0c4c7d033dba"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("5d585d78-945b-41bb-9d92-c98a06b31d45"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("63845308-fea9-4e75-a3ee-e6690ef0f690"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("64cae9f5-fced-4c5e-be14-2c97eefe5ce4"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("66c313a8-bcf0-424d-b71e-8118132f8d2a"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("78add384-59a1-4df0-b862-a04206825f3f"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("7e3bbe14-7cdf-4d95-a58b-f77b196f3e98"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("a5773b18-d2d9-4fe9-a5ac-23d9aa9a8a6a"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("a6ce5a4c-5299-427d-a98f-d8d1d335f05d"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("cc038d6b-3739-4980-896c-f7af9c5275c8"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("d9117560-ce79-4e1a-aa42-15af2afbbe89"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("eb725059-2702-4495-a1da-2ffa205e8cc5"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("ede7cad4-ac6e-4c3d-8ee5-a121836c3d84"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") },
                    { new Guid("fbb3fddb-e1b9-4bab-9dc0-4c46e4a1fe14"), new Guid("8b1bea78-5520-46e9-a6c9-cb16a152b5aa") }
                });
        }
    }
}
