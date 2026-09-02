using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWerkzeugAusleiheHistorie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WerkzeugAusleihen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WerkzeugId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BorrowedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedReturnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WerkzeugAusleihen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WerkzeugAusleihen_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WerkzeugAusleihen_Werkzeuge_WerkzeugId",
                        column: x => x.WerkzeugId,
                        principalTable: "Werkzeuge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WerkzeugAusleihen_UserId",
                table: "WerkzeugAusleihen",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WerkzeugAusleihen_WerkzeugId_BorrowedAt",
                table: "WerkzeugAusleihen",
                columns: new[] { "WerkzeugId", "BorrowedAt" });

            // Aktuell ausgeliehene Werkzeuge als offenen Historien-Eintrag übernehmen.
            migrationBuilder.Sql(
                """
                INSERT INTO "WerkzeugAusleihen" ("Id", "WerkzeugId", "UserId", "BorrowedAt", "ExpectedReturnAt", "ReturnedAt")
                SELECT gen_random_uuid(), "Id", "BorrowedByUserId", "BorrowedAt",
                       COALESCE("ExpectedReturnAt", "BorrowedAt"), NULL
                FROM "Werkzeuge"
                WHERE "IsAvailable" = FALSE
                  AND "BorrowedByUserId" IS NOT NULL
                  AND "BorrowedAt" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WerkzeugAusleihen");
        }
    }
}
