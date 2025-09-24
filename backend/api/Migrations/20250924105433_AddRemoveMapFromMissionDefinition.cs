using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRemoveMapFromMissionDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Map_Boundary_X1",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_X2",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_Y1",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_Y2",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_Z1",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_Z2",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Map_MapName",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Map_TransformationMatrices_C1",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Map_TransformationMatrices_C2",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Map_TransformationMatrices_D1",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Map_TransformationMatrices_D2",
                table: "MissionDefinitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_X1",
                table: "MissionDefinitions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_X2",
                table: "MissionDefinitions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Y1",
                table: "MissionDefinitions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Y2",
                table: "MissionDefinitions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Z1",
                table: "MissionDefinitions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Z2",
                table: "MissionDefinitions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Map_MapName",
                table: "MissionDefinitions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_C1",
                table: "MissionDefinitions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_C2",
                table: "MissionDefinitions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_D1",
                table: "MissionDefinitions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_D2",
                table: "MissionDefinitions",
                type: "double precision",
                nullable: true);
        }
    }
}
