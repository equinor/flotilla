using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeAreaPolygonOwned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AreaPolygonJson",
                table: "InspectionAreas",
                newName: "AreaPolygon_Positions");

            migrationBuilder.AddColumn<double>(
                name: "AreaPolygon_ZMax",
                table: "InspectionAreas",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AreaPolygon_ZMin",
                table: "InspectionAreas",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaPolygon_ZMax",
                table: "InspectionAreas");

            migrationBuilder.DropColumn(
                name: "AreaPolygon_ZMin",
                table: "InspectionAreas");

            migrationBuilder.RenameColumn(
                name: "AreaPolygon_Positions",
                table: "InspectionAreas",
                newName: "AreaPolygonJson");
        }
    }
}
