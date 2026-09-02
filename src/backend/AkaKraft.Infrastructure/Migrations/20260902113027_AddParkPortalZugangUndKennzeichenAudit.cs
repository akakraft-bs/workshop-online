using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParkPortalZugangUndKennzeichenAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortalPassword",
                table: "ParkAccounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalUsername",
                table: "ParkAccounts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParkKennzeichenAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParkAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AusgefuehrtVon = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Aktion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Kennzeichen = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    KennzeichenNachher = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkKennzeichenAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkKennzeichenAudits_ParkAccounts_ParkAccountId",
                        column: x => x.ParkAccountId,
                        principalTable: "ParkAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParkKennzeichenAudits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "ParkAccounts",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-0000000000a1"),
                columns: new[] { "PortalPassword", "PortalUsername" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ParkAccounts",
                keyColumn: "Id",
                keyValue: new Guid("b2b2b2b2-0000-0000-0000-0000000000b2"),
                columns: new[] { "PortalPassword", "PortalUsername" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_ParkKennzeichenAudits_ParkAccountId_CreatedAt",
                table: "ParkKennzeichenAudits",
                columns: new[] { "ParkAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkKennzeichenAudits_UserId",
                table: "ParkKennzeichenAudits",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkKennzeichenAudits");

            migrationBuilder.DropColumn(
                name: "PortalPassword",
                table: "ParkAccounts");

            migrationBuilder.DropColumn(
                name: "PortalUsername",
                table: "ParkAccounts");
        }
    }
}
