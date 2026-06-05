using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWerkzeugAnleitung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AnleitungDokumentId",
                table: "Werkzeuge",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Werkzeuge_AnleitungDokumentId",
                table: "Werkzeuge",
                column: "AnleitungDokumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Werkzeuge_Dokumente_AnleitungDokumentId",
                table: "Werkzeuge",
                column: "AnleitungDokumentId",
                principalTable: "Dokumente",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Werkzeuge_Dokumente_AnleitungDokumentId",
                table: "Werkzeuge");

            migrationBuilder.DropIndex(
                name: "IX_Werkzeuge_AnleitungDokumentId",
                table: "Werkzeuge");

            migrationBuilder.DropColumn(
                name: "AnleitungDokumentId",
                table: "Werkzeuge");
        }
    }
}
