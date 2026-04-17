using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkaKraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMangelImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Maengel",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Maengel");
        }
    }
}
