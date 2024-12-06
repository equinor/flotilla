using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEchoTagTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "IsarZoomDescription_Height",
                table: "MissionTasks",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IsarZoomDescription_Width",
                table: "MissionTasks",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EchoTagInspectionMetadata",
                columns: table => new
                {
                    TagId = table.Column<string>(type: "text", nullable: false),
                    ZoomDescription_Width = table.Column<double>(type: "double precision", nullable: true),
                    ZoomDescription_Height = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EchoTagInspectionMetadata", x => x.TagId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EchoTagInspectionMetadata");

            migrationBuilder.DropColumn(
                name: "IsarZoomDescription_Height",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "IsarZoomDescription_Width",
                table: "MissionTasks");
        }
    }
}
