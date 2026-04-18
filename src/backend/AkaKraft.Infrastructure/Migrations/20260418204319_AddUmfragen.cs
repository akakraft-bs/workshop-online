using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUmfragen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyUmfragen",
                table: "UserPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Umfragen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsMultipleChoice = table.Column<bool>(type: "boolean", nullable: false),
                    ResultsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    RevealAfterClose = table.Column<bool>(type: "boolean", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadlineReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Umfragen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Umfragen_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Umfragen_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UmfrageOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UmfrageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UmfrageOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UmfrageOptions_Umfragen_UmfrageId",
                        column: x => x.UmfrageId,
                        principalTable: "Umfragen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UmfrageAntworten",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UmfrageId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UmfrageAntworten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UmfrageAntworten_UmfrageOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "UmfrageOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UmfrageAntworten_Umfragen_UmfrageId",
                        column: x => x.UmfrageId,
                        principalTable: "Umfragen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UmfrageAntworten_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UmfrageAntworten_OptionId",
                table: "UmfrageAntworten",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UmfrageAntworten_UmfrageId_OptionId_UserId",
                table: "UmfrageAntworten",
                columns: new[] { "UmfrageId", "OptionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UmfrageAntworten_UserId",
                table: "UmfrageAntworten",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Umfragen_ClosedByUserId",
                table: "Umfragen",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Umfragen_CreatedByUserId",
                table: "Umfragen",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UmfrageOptions_UmfrageId",
                table: "UmfrageOptions",
                column: "UmfrageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UmfrageAntworten");

            migrationBuilder.DropTable(
                name: "UmfrageOptions");

            migrationBuilder.DropTable(
                name: "Umfragen");

            migrationBuilder.DropColumn(
                name: "NotifyUmfragen",
                table: "UserPreferences");
        }
    }
}
