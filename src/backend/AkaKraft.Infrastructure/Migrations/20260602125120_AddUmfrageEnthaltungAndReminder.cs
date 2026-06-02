using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUmfrageEnthaltungAndReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastManualReminderSentAt",
                table: "Umfragen",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UmfrageEnthaltungen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UmfrageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AbstainedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UmfrageEnthaltungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UmfrageEnthaltungen_Umfragen_UmfrageId",
                        column: x => x.UmfrageId,
                        principalTable: "Umfragen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UmfrageEnthaltungen_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UmfrageEnthaltungen_UmfrageId_UserId",
                table: "UmfrageEnthaltungen",
                columns: new[] { "UmfrageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UmfrageEnthaltungen_UserId",
                table: "UmfrageEnthaltungen",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UmfrageEnthaltungen");

            migrationBuilder.DropColumn(
                name: "LastManualReminderSentAt",
                table: "Umfragen");
        }
    }
}
