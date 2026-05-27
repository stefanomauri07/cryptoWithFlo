using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlan",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "SubscriptionPlan" },
                values: new object[] { "$2a$11$lBDHNPfKCWwErR92MAMgKeXXPNwZW5sSn5XuJggawjsEEVqrQEFtq", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionPlan",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$XvPd2drc1vnfTlf38xDloOGcm2dMY73t6XsUjtxqnVaMV8nkC3tQ.");
        }
    }
}
