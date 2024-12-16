using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class UseInspectionAreaInsteadOfAreaForLocalisation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_Areas_AreaId",
                table: "MissionDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_Areas_AreaId",
                table: "MissionRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Areas_CurrentAreaId",
                table: "Robots");

            migrationBuilder.DropIndex(
                name: "IX_MissionRuns_AreaId",
                table: "MissionRuns");

            migrationBuilder.DropIndex(
                name: "IX_MissionDefinitions_AreaId",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_X1",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_X2",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_Y1",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_Y2",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_Z1",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_Boundary_Z2",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_MapName",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_TransformationMatrices_C1",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_TransformationMatrices_C2",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_TransformationMatrices_D1",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "Map_TransformationMatrices_D2",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "MissionDefinitions");

            migrationBuilder.RenameColumn(
                name: "CurrentAreaId",
                table: "Robots",
                newName: "CurrentInspectionAreaId");

            migrationBuilder.RenameIndex(
                name: "IX_Robots_CurrentAreaId",
                table: "Robots",
                newName: "IX_Robots_CurrentInspectionAreaId");

            migrationBuilder.AddColumn<string>(
                name: "InspectionAreaId",
                table: "MissionRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionAreaId",
                table: "MissionDefinitions",
                type: "text",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_MissionRuns_InspectionAreaId",
                table: "MissionRuns",
                column: "InspectionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinitions_InspectionAreaId",
                table: "MissionDefinitions",
                column: "InspectionAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_Decks_InspectionAreaId",
                table: "MissionDefinitions",
                column: "InspectionAreaId",
                principalTable: "Decks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRuns_Decks_InspectionAreaId",
                table: "MissionRuns",
                column: "InspectionAreaId",
                principalTable: "Decks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Decks_CurrentInspectionAreaId",
                table: "Robots",
                column: "CurrentInspectionAreaId",
                principalTable: "Decks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_Decks_InspectionAreaId",
                table: "MissionDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_Decks_InspectionAreaId",
                table: "MissionRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Decks_CurrentInspectionAreaId",
                table: "Robots");

            migrationBuilder.DropIndex(
                name: "IX_MissionRuns_InspectionAreaId",
                table: "MissionRuns");

            migrationBuilder.DropIndex(
                name: "IX_MissionDefinitions_InspectionAreaId",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "InspectionAreaId",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "InspectionAreaId",
                table: "MissionDefinitions");

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

            migrationBuilder.RenameColumn(
                name: "CurrentInspectionAreaId",
                table: "Robots",
                newName: "CurrentAreaId");

            migrationBuilder.RenameIndex(
                name: "IX_Robots_CurrentInspectionAreaId",
                table: "Robots",
                newName: "IX_Robots_CurrentAreaId");

            migrationBuilder.AddColumn<string>(
                name: "AreaId",
                table: "MissionRuns",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_X1",
                table: "MissionRuns",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_X2",
                table: "MissionRuns",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Y1",
                table: "MissionRuns",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Y2",
                table: "MissionRuns",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Z1",
                table: "MissionRuns",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Z2",
                table: "MissionRuns",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Map_MapName",
                table: "MissionRuns",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_C1",
                table: "MissionRuns",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_C2",
                table: "MissionRuns",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_D1",
                table: "MissionRuns",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_D2",
                table: "MissionRuns",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AreaId",
                table: "MissionDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MissionRuns_AreaId",
                table: "MissionRuns",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinitions_AreaId",
                table: "MissionDefinitions",
                column: "AreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_Areas_AreaId",
                table: "MissionDefinitions",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRuns_Areas_AreaId",
                table: "MissionRuns",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Areas_CurrentAreaId",
                table: "Robots",
                column: "CurrentAreaId",
                principalTable: "Areas",
                principalColumn: "Id");
        }
    }
}
