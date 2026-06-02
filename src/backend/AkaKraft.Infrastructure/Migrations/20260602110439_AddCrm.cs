using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCrm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Partner",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Kategorie = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Website = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Notizen = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partner", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ansprechpartner",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Position = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Notizen = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ansprechpartner", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ansprechpartner_Partner_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kontakteintraege",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnsprechpartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Datum = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kanal = table.Column<string>(type: "text", nullable: false),
                    Reaktion = table.Column<string>(type: "text", nullable: false),
                    Zusammenfassung = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    NaechsteSchritte = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kontakteintraege", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kontakteintraege_Ansprechpartner_AnsprechpartnerId",
                        column: x => x.AnsprechpartnerId,
                        principalTable: "Ansprechpartner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Kontakteintraege_Partner_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ansprechpartner_PartnerId",
                table: "Ansprechpartner",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Kontakteintraege_AnsprechpartnerId",
                table: "Kontakteintraege",
                column: "AnsprechpartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Kontakteintraege_PartnerId",
                table: "Kontakteintraege",
                column: "PartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kontakteintraege");

            migrationBuilder.DropTable(
                name: "Ansprechpartner");

            migrationBuilder.DropTable(
                name: "Partner");
        }
    }
}
