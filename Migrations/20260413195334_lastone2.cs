using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriskyApi.Migrations
{
    /// <inheritdoc />
    public partial class lastone2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "IsActive", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 13, 19, 53, 34, 236, DateTimeKind.Utc).AddTicks(8375), true, "$2a$11$wPlPOmnM0KotIniqI15AFeuxW1WVoIBGxj8lC/WN8CuG2uW6mtLE.", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$DgpvHxNvNM.RxfxQUsV/gOnCvcAPwVTsKKn2Zz.QogGfvJbHmuzPG");
        }
    }
}
