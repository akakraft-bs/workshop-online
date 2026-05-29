using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Wuensche",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Werkzeuge",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Verbrauchsmaterialien",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Umfragen",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Projekte",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Maengel",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "HallenbuchEintraege",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Aufgaben",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Wuensche");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Werkzeuge");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Verbrauchsmaterialien");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Umfragen");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Projekte");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Maengel");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "HallenbuchEintraege");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Aufgaben");
        }
    }
}
