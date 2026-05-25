using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CryptoApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUserHoldings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserHoldings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CryptoId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHoldings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserHoldings_TrackedCryptos_CryptoId",
                        column: x => x.CryptoId,
                        principalTable: "TrackedCryptos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserHoldings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "UserHoldings",
                columns: new[] { "Id", "Amount", "CreatedAt", "CryptoId", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { 1, 0m, new DateTime(2026, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), "bitcoin", null, 1 },
                    { 2, 0m, new DateTime(2026, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), "ethereum", null, 1 },
                    { 3, 0m, new DateTime(2026, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), "solana", null, 1 }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$d2DWWC06Wg61PQeB7NNZQerBMNczgLFau8rpqR65gVf8pYmcilJ1W");

            migrationBuilder.CreateIndex(
                name: "IX_UserHoldings_CryptoId",
                table: "UserHoldings",
                column: "CryptoId");

            migrationBuilder.CreateIndex(
                name: "IX_UserHoldings_UserId_CryptoId",
                table: "UserHoldings",
                columns: new[] { "UserId", "CryptoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserHoldings");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$eMeyLAaI42VoMxI5ZzcMvOyw4WEEiA/uhsaHEbmBnATS0n6TYvhge");
        }
    }
}
