using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTagInspectionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TagInspectionMetadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TagInspectionMetadata",
                columns: table => new
                {
                    TagId = table.Column<string>(type: "text", nullable: false),
                    ZoomDescription_ObjectHeight = table.Column<double>(type: "double precision", nullable: true),
                    ZoomDescription_ObjectWidth = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagInspectionMetadata", x => x.TagId);
                });
        }
    }
}
