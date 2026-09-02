using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHallenbuchFahrzeug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FahrzeugId",
                table: "HallenbuchEintraege",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FahrzeugLabel",
                table: "HallenbuchEintraege",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FahrzeugId",
                table: "HallenbuchEintraege");

            migrationBuilder.DropColumn(
                name: "FahrzeugLabel",
                table: "HallenbuchEintraege");
        }
    }
}
