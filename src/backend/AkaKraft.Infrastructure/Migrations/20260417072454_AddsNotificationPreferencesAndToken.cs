using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddsNotificationPreferencesAndToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyLeihruckgabe",
                table: "UserPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyMindestbestand",
                table: "UserPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyVeranstaltungen",
                table: "UserPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FcmTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FcmTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FcmTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FcmTokens_Token",
                table: "FcmTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FcmTokens_UserId",
                table: "FcmTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FcmTokens");

            migrationBuilder.DropColumn(
                name: "NotifyLeihruckgabe",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "NotifyMindestbestand",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "NotifyVeranstaltungen",
                table: "UserPreferences");
        }
    }
}
