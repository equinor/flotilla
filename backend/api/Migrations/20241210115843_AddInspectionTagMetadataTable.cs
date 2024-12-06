using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionTagMetadataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "IsarZoomDescription_ObjectHeight",
                table: "MissionTasks",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IsarZoomDescription_ObjectWidth",
                table: "MissionTasks",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TagInspectionMetadata",
                columns: table => new
                {
                    TagId = table.Column<string>(type: "text", nullable: false),
                    ZoomDescription_ObjectWidth = table.Column<double>(type: "double precision", nullable: true),
                    ZoomDescription_ObjectHeight = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagInspectionMetadata", x => x.TagId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TagInspectionMetadata");

            migrationBuilder.DropColumn(
                name: "IsarZoomDescription_ObjectHeight",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "IsarZoomDescription_ObjectWidth",
                table: "MissionTasks");
        }
    }
}
