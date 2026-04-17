using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NumberArtist.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReferenceDrawingUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "ReferenceDrawings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "ReferenceDrawings");
        }
    }
}
