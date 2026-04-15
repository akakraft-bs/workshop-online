using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddsCalendarConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoogleCalendarId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalendarWriteRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarWriteRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarWriteRoles_CalendarConfigs_CalendarConfigId",
                        column: x => x.CalendarConfigId,
                        principalTable: "CalendarConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarConfigs_GoogleCalendarId",
                table: "CalendarConfigs",
                column: "GoogleCalendarId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarWriteRoles_CalendarConfigId_Role",
                table: "CalendarWriteRoles",
                columns: new[] { "CalendarConfigId", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CalendarWriteRoles");
            migrationBuilder.DropTable(name: "CalendarConfigs");
        }
    }
}
