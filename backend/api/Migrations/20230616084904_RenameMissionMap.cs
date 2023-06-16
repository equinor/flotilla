using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class RenameMissionMap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Map_TransformationMatrices_D2",
                table: "Missions",
                newName: "MapMetadata_TransformationMatrices_D2");

            migrationBuilder.RenameColumn(
                name: "Map_TransformationMatrices_D1",
                table: "Missions",
                newName: "MapMetadata_TransformationMatrices_D1");

            migrationBuilder.RenameColumn(
                name: "Map_TransformationMatrices_C2",
                table: "Missions",
                newName: "MapMetadata_TransformationMatrices_C2");

            migrationBuilder.RenameColumn(
                name: "Map_TransformationMatrices_C1",
                table: "Missions",
                newName: "MapMetadata_TransformationMatrices_C1");

            migrationBuilder.RenameColumn(
                name: "Map_MapName",
                table: "Missions",
                newName: "MapMetadata_MapName");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_Z2",
                table: "Missions",
                newName: "MapMetadata_Boundary_Z2");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_Z1",
                table: "Missions",
                newName: "MapMetadata_Boundary_Z1");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_Y2",
                table: "Missions",
                newName: "MapMetadata_Boundary_Y2");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_Y1",
                table: "Missions",
                newName: "MapMetadata_Boundary_Y1");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_X2",
                table: "Missions",
                newName: "MapMetadata_Boundary_X2");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_X1",
                table: "Missions",
                newName: "MapMetadata_Boundary_X1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MapMetadata_TransformationMatrices_D2",
                table: "Missions",
                newName: "Map_TransformationMatrices_D2");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_TransformationMatrices_D1",
                table: "Missions",
                newName: "Map_TransformationMatrices_D1");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_TransformationMatrices_C2",
                table: "Missions",
                newName: "Map_TransformationMatrices_C2");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_TransformationMatrices_C1",
                table: "Missions",
                newName: "Map_TransformationMatrices_C1");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_MapName",
                table: "Missions",
                newName: "Map_MapName");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_Z2",
                table: "Missions",
                newName: "Map_Boundary_Z2");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_Z1",
                table: "Missions",
                newName: "Map_Boundary_Z1");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_Y2",
                table: "Missions",
                newName: "Map_Boundary_Y2");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_Y1",
                table: "Missions",
                newName: "Map_Boundary_Y1");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_X2",
                table: "Missions",
                newName: "Map_Boundary_X2");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_X1",
                table: "Missions",
                newName: "Map_Boundary_X1");
        }
    }
}
