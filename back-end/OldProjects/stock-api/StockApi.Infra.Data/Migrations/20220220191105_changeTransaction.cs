using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockApi.Infra.Data.Migrations
{
    public partial class changeTransaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "date",
                table: "ProductTransaction",
                newName: "Date");

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "ProductTransaction",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ProductTransaction_DeviceId",
                table: "ProductTransaction",
                column: "DeviceId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductTransaction_DeviceId",
                table: "ProductTransaction");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "ProductTransaction");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "ProductTransaction",
                newName: "date");
        }
    }
}
