using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParkplatzUndFahrzeuge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GrantsParkplatzBerechtigung",
                table: "CalendarConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Fahrzeuge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Marke = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Modell = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Kennzeichen = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IstStandard = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fahrzeuge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fahrzeuge_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParkAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PortalUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Notiz = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParkClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParkAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kennzeichen = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    FahrzeugBezeichnung = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EinfahrtAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VoraussichtlichBis = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FreigegebenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutoExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BerechtigungArt = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BestaetigungHinweis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BookingEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Erinnerung2hGesendet = table.Column<bool>(type: "boolean", nullable: false),
                    ErinnerungAblaufGesendet = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkClaims_ParkAccounts_ParkAccountId",
                        column: x => x.ParkAccountId,
                        principalTable: "ParkAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParkClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ParkAccounts",
                columns: new[] { "Id", "Label", "Notiz", "PortalUrl", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("a1a1a1a1-0000-0000-0000-0000000000a1"), "Parkkonto A", null, null, 0 },
                    { new Guid("b2b2b2b2-0000-0000-0000-0000000000b2"), "Parkkonto B", null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fahrzeuge_UserId",
                table: "Fahrzeuge",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkClaims_ParkAccountId_FreigegebenAt",
                table: "ParkClaims",
                columns: new[] { "ParkAccountId", "FreigegebenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkClaims_UserId",
                table: "ParkClaims",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fahrzeuge");

            migrationBuilder.DropTable(
                name: "ParkClaims");

            migrationBuilder.DropTable(
                name: "ParkAccounts");

            migrationBuilder.DropColumn(
                name: "GrantsParkplatzBerechtigung",
                table: "CalendarConfigs");
        }
    }
}
