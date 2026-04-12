using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddsInventarTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Verbrauchsmaterialien",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    MinQuantity = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verbrauchsmaterialien", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Werkzeuge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Dimensions = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    BorrowedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BorrowedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Werkzeuge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Werkzeuge_Users_BorrowedByUserId",
                        column: x => x.BorrowedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Werkzeuge_BorrowedByUserId",
                table: "Werkzeuge",
                column: "BorrowedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Verbrauchsmaterialien");

            migrationBuilder.DropTable(
                name: "Werkzeuge");
        }
    }
}
