using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class AddElevationToMapBoundary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Z1",
                table: "Missions",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Z2",
                table: "Missions",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Map_Boundary_Z1",
                table: "Missions");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_Z2",
                table: "Missions");
        }
    }
}
