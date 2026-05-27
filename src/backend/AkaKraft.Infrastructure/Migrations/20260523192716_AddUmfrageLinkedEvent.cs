using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUmfrageLinkedEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedCalendarId",
                table: "Umfragen",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedEventId",
                table: "Umfragen",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LinkedEventStart",
                table: "Umfragen",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedEventTitle",
                table: "Umfragen",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkedCalendarId",
                table: "Umfragen");

            migrationBuilder.DropColumn(
                name: "LinkedEventId",
                table: "Umfragen");

            migrationBuilder.DropColumn(
                name: "LinkedEventStart",
                table: "Umfragen");

            migrationBuilder.DropColumn(
                name: "LinkedEventTitle",
                table: "Umfragen");
        }
    }
}
