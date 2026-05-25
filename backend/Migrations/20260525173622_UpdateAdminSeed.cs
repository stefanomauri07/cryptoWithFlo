using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "PasswordHash" },
                values: new object[] { "admin", "$2a$11$eMeyLAaI42VoMxI5ZzcMvOyw4WEEiA/uhsaHEbmBnATS0n6TYvhge" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "PasswordHash" },
                values: new object[] { "admin@cryptotracker.com", "$2a$11$BAj6C8HgWxNoVEwedjwpR.vV9KzvGNZMg2YHmTEaM03VCxSERaNIe" });
        }
    }
}
