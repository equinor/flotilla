using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameDeckTableToInspectionArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Decks_InspectionAreaId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_DefaultLocalizationPoses_DefaultLocalizationPoseId",
                table: "Decks");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_Installations_InstallationId",
                table: "Decks");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_Plants_PlantId",
                table: "Decks");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_Decks_InspectionAreaId",
                table: "MissionDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_Decks_InspectionAreaId",
                table: "MissionRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Decks_CurrentInspectionAreaId",
                table: "Robots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Decks",
                table: "Decks");

            migrationBuilder.RenameTable(
                name: "Decks",
                newName: "InspectionAreas");

            migrationBuilder.RenameIndex(
                name: "IX_Decks_PlantId",
                table: "InspectionAreas",
                newName: "IX_InspectionAreas_PlantId");

            migrationBuilder.RenameIndex(
                name: "IX_Decks_InstallationId",
                table: "InspectionAreas",
                newName: "IX_InspectionAreas_InstallationId");

            migrationBuilder.RenameIndex(
                name: "IX_Decks_DefaultLocalizationPoseId",
                table: "InspectionAreas",
                newName: "IX_InspectionAreas_DefaultLocalizationPoseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InspectionAreas",
                table: "InspectionAreas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_InspectionAreas_InspectionAreaId",
                table: "Areas",
                column: "InspectionAreaId",
                principalTable: "InspectionAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionAreas_DefaultLocalizationPoses_DefaultLocalizatio~",
                table: "InspectionAreas",
                column: "DefaultLocalizationPoseId",
                principalTable: "DefaultLocalizationPoses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionAreas_Installations_InstallationId",
                table: "InspectionAreas",
                column: "InstallationId",
                principalTable: "Installations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionAreas_Plants_PlantId",
                table: "InspectionAreas",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_InspectionAreas_InspectionAreaId",
                table: "MissionDefinitions",
                column: "InspectionAreaId",
                principalTable: "InspectionAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRuns_InspectionAreas_InspectionAreaId",
                table: "MissionRuns",
                column: "InspectionAreaId",
                principalTable: "InspectionAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_InspectionAreas_CurrentInspectionAreaId",
                table: "Robots",
                column: "CurrentInspectionAreaId",
                principalTable: "InspectionAreas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_InspectionAreas_InspectionAreaId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionAreas_DefaultLocalizationPoses_DefaultLocalizatio~",
                table: "InspectionAreas");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionAreas_Installations_InstallationId",
                table: "InspectionAreas");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionAreas_Plants_PlantId",
                table: "InspectionAreas");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_InspectionAreas_InspectionAreaId",
                table: "MissionDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_InspectionAreas_InspectionAreaId",
                table: "MissionRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_InspectionAreas_CurrentInspectionAreaId",
                table: "Robots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InspectionAreas",
                table: "InspectionAreas");

            migrationBuilder.RenameTable(
                name: "InspectionAreas",
                newName: "Decks");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionAreas_PlantId",
                table: "Decks",
                newName: "IX_Decks_PlantId");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionAreas_InstallationId",
                table: "Decks",
                newName: "IX_Decks_InstallationId");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionAreas_DefaultLocalizationPoseId",
                table: "Decks",
                newName: "IX_Decks_DefaultLocalizationPoseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Decks",
                table: "Decks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Decks_InspectionAreaId",
                table: "Areas",
                column: "InspectionAreaId",
                principalTable: "Decks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_DefaultLocalizationPoses_DefaultLocalizationPoseId",
                table: "Decks",
                column: "DefaultLocalizationPoseId",
                principalTable: "DefaultLocalizationPoses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_Installations_InstallationId",
                table: "Decks",
                column: "InstallationId",
                principalTable: "Installations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_Plants_PlantId",
                table: "Decks",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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
    }
}
