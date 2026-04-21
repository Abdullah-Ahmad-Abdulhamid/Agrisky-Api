using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriskyApi.Migrations
{
    /// <inheritdoc />
    public partial class lastsecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiryDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProofImagePath",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionReference",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "CreatedAt", "Email", "LastName", "PasswordHash", "PhoneNumber", "RefreshToken", "RefreshTokenExpiryDate" },
                values: new object[] { null, new DateTime(2026, 4, 21, 18, 41, 18, 188, DateTimeKind.Utc).AddTicks(2000), "admin@agrisky.app", "AgriSky", "$2a$11$zV7YR3qkEQQC6DZvUQzdcuTQ9x3uhy6eDfjTNTjIwygofXHZ/qEAW", null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiryDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TransactionReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "ProofImagePath",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "CreatedAt", "Email", "LastName", "PasswordHash", "PhoneNumber" },
                values: new object[] { "Admin Address", new DateTime(2026, 4, 13, 19, 53, 34, 236, DateTimeKind.Utc).AddTicks(8375), "admin@agrisky.com", "User", "$2a$11$wPlPOmnM0KotIniqI15AFeuxW1WVoIBGxj8lC/WN8CuG2uW6mtLE.", "01000000000" });
        }
    }
}
