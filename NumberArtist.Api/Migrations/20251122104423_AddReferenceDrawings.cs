using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NumberArtist.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceDrawings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReferenceDrawingId",
                table: "DxfFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReferenceDrawings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DxfFileId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceDrawings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferenceDrawings");

            migrationBuilder.DropColumn(
                name: "ReferenceDrawingId",
                table: "DxfFiles");
        }
    }
}
