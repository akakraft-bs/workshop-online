using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVerbrauchsmaterialNachbestellt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNachbestellt",
                table: "Verbrauchsmaterialien",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NachbestelltAt",
                table: "Verbrauchsmaterialien",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NachbestelltVonName",
                table: "Verbrauchsmaterialien",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNachbestellt",
                table: "Verbrauchsmaterialien");

            migrationBuilder.DropColumn(
                name: "NachbestelltAt",
                table: "Verbrauchsmaterialien");

            migrationBuilder.DropColumn(
                name: "NachbestelltVonName",
                table: "Verbrauchsmaterialien");
        }
    }
}
