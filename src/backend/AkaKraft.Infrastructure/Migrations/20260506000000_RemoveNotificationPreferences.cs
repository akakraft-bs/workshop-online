using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "NotifyLeihruckgabe", table: "UserPreferences");
            migrationBuilder.DropColumn(name: "NotifyVeranstaltungen", table: "UserPreferences");
            migrationBuilder.DropColumn(name: "NotifyMindestbestand", table: "UserPreferences");
            migrationBuilder.DropColumn(name: "NotifyUmfragen", table: "UserPreferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "NotifyLeihruckgabe", table: "UserPreferences",
                type: "boolean", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<bool>(name: "NotifyVeranstaltungen", table: "UserPreferences",
                type: "boolean", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<bool>(name: "NotifyMindestbestand", table: "UserPreferences",
                type: "boolean", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<bool>(name: "NotifyUmfragen", table: "UserPreferences",
                type: "boolean", nullable: false, defaultValue: true);
        }
    }
}
