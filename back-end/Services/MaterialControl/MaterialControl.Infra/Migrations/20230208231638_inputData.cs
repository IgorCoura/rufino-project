using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MaterialControl.Infra.Migrations
{
    /// <inheritdoc />
    public partial class inputData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FunctionIdRole",
                keyColumns: new[] { "FunctionsIdsId", "RolesId" },
                keyValues: new object[] { new Guid("a0a30d8f-54dc-497b-90c7-0b21c379a517"), new Guid("a952e946-8b19-41fc-92d8-9abad5893d0e") });

            migrationBuilder.DeleteData(
                table: "FunctionsIds",
                keyColumn: "Id",
                keyValue: new Guid("a0a30d8f-54dc-497b-90c7-0b21c379a517"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a952e946-8b19-41fc-92d8-9abad5893d0e"));

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                values: new object[] { new Guid("a0a30d8f-54dc-497b-90c7-0b21c379a517"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2999", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new Guid("a952e946-8b19-41fc-92d8-9abad5893d0e"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "FunctionIdRole",
                columns: new[] { "FunctionsIdsId", "RolesId" },
                values: new object[] { new Guid("a0a30d8f-54dc-497b-90c7-0b21c379a517"), new Guid("a952e946-8b19-41fc-92d8-9abad5893d0e") });
        }
    }
}
