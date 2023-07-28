using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesToPluralAndRemoveAssetDeck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Area_Deck_DeckId",
                table: "Area");

            migrationBuilder.DropForeignKey(
                name: "FK_Area_Installation_InstallationId",
                table: "Area");

            migrationBuilder.DropForeignKey(
                name: "FK_Area_Plant_PlantId",
                table: "Area");

            migrationBuilder.DropForeignKey(
                name: "FK_Deck_Installation_InstallationId",
                table: "Deck");

            migrationBuilder.DropForeignKey(
                name: "FK_Deck_Plant_PlantId",
                table: "Deck");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinition_Area_AreaId",
                table: "MissionDefinition");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinition_MissionRun_LastRunId",
                table: "MissionDefinition");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinition_Source_SourceId",
                table: "MissionDefinition");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionTask_MissionRun_MissionRunId",
                table: "MissionTask");

            migrationBuilder.DropForeignKey(
                name: "FK_Plant_Installation_InstallationId",
                table: "Plant");

            migrationBuilder.DropForeignKey(
                name: "FK_Robot_Area_CurrentAreaId",
                table: "Robot");

            migrationBuilder.DropForeignKey(
                name: "FK_Robot_RobotModel_ModelId",
                table: "Robot");

            migrationBuilder.DropForeignKey(
                name: "FK_SafePosition_Area_AreaId",
                table: "SafePosition");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoStream_Robot_RobotId",
                table: "VideoStream");

            migrationBuilder.DropTable(
                name: "Area.DefaultLocalizationPose#Pose.Orientation#Orientation");

            migrationBuilder.DropTable(
                name: "Area.DefaultLocalizationPose#Pose.Position#Position");

            migrationBuilder.DropTable(
                name: "Area.MapMetadata#MapMetadata.Boundary#Boundary");

            migrationBuilder.DropTable(
                name: "Area.MapMetadata#MapMetadata.TransformationMatrices#TransformationMatrices");

            migrationBuilder.DropTable(
                name: "AssetDeck");

            migrationBuilder.DropTable(
                name: "MissionRun.MapMetadata#MapMetadata.Boundary#Boundary");

            migrationBuilder.DropTable(
                name: "MissionRun.MapMetadata#MapMetadata.TransformationMatrices#TransformationMatrices");

            migrationBuilder.DropTable(
                name: "MissionTask.InspectionTarget#Position");

            migrationBuilder.DropTable(
                name: "MissionTask.RobotPose#Pose.Orientation#Orientation");

            migrationBuilder.DropTable(
                name: "MissionTask.RobotPose#Pose.Position#Position");

            migrationBuilder.DropTable(
                name: "Robot.Pose#Pose.Orientation#Orientation");

            migrationBuilder.DropTable(
                name: "Robot.Pose#Pose.Position#Position");

            migrationBuilder.DropTable(
                name: "SafePosition.Pose#Pose.Orientation#Orientation");

            migrationBuilder.DropTable(
                name: "SafePosition.Pose#Pose.Position#Position");

            migrationBuilder.DropTable(
                name: "Area.DefaultLocalizationPose#Pose");

            migrationBuilder.DropTable(
                name: "Area.MapMetadata#MapMetadata");

            migrationBuilder.DropTable(
                name: "MissionRun.MapMetadata#MapMetadata");

            migrationBuilder.DropTable(
                name: "MissionTask.RobotPose#Pose");

            migrationBuilder.DropTable(
                name: "Robot.Pose#Pose");

            migrationBuilder.DropTable(
                name: "SafePosition.Pose#Pose");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Source",
                table: "Source");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SafePosition",
                table: "SafePosition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RobotModel",
                table: "RobotModel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Robot",
                table: "Robot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Plant",
                table: "Plant");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MissionDefinition",
                table: "MissionDefinition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Installation",
                table: "Installation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Deck",
                table: "Deck");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Area",
                table: "Area");

            migrationBuilder.RenameTable(
                name: "Source",
                newName: "Sources");

            migrationBuilder.RenameTable(
                name: "SafePosition",
                newName: "SafePositions");

            migrationBuilder.RenameTable(
                name: "RobotModel",
                newName: "RobotModels");

            migrationBuilder.RenameTable(
                name: "Robot",
                newName: "Robots");

            migrationBuilder.RenameTable(
                name: "Plant",
                newName: "Plants");

            migrationBuilder.RenameTable(
                name: "MissionRun",
                newName: "MissionRuns");

            migrationBuilder.RenameTable(
                name: "MissionDefinition",
                newName: "MissionDefinitions");

            migrationBuilder.RenameTable(
                name: "Installation",
                newName: "Installations");

            migrationBuilder.RenameTable(
                name: "Deck",
                newName: "Decks");

            migrationBuilder.RenameTable(
                name: "Area",
                newName: "Areas");

            migrationBuilder.RenameIndex(
                name: "IX_SafePosition_AreaId",
                table: "SafePositions",
                newName: "IX_SafePositions_AreaId");

            migrationBuilder.RenameIndex(
                name: "IX_RobotModel_Type",
                table: "RobotModels",
                newName: "IX_RobotModels_Type");

            migrationBuilder.RenameIndex(
                name: "IX_Robot_ModelId",
                table: "Robots",
                newName: "IX_Robots_ModelId");

            migrationBuilder.RenameIndex(
                name: "IX_Robot_CurrentAreaId",
                table: "Robots",
                newName: "IX_Robots_CurrentAreaId");

            migrationBuilder.RenameIndex(
                name: "IX_Plant_PlantCode",
                table: "Plants",
                newName: "IX_Plants_PlantCode");

            migrationBuilder.RenameIndex(
                name: "IX_Plant_InstallationId",
                table: "Plants",
                newName: "IX_Plants_InstallationId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionRun_RobotId",
                table: "MissionRuns",
                newName: "IX_MissionRuns_RobotId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionRun_AreaId",
                table: "MissionRuns",
                newName: "IX_MissionRuns_AreaId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionDefinition_SourceId",
                table: "MissionDefinitions",
                newName: "IX_MissionDefinitions_SourceId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionDefinition_LastRunId",
                table: "MissionDefinitions",
                newName: "IX_MissionDefinitions_LastRunId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionDefinition_AreaId",
                table: "MissionDefinitions",
                newName: "IX_MissionDefinitions_AreaId");

            migrationBuilder.RenameIndex(
                name: "IX_Installation_InstallationCode",
                table: "Installations",
                newName: "IX_Installations_InstallationCode");

            migrationBuilder.RenameIndex(
                name: "IX_Deck_PlantId",
                table: "Decks",
                newName: "IX_Decks_PlantId");

            migrationBuilder.RenameIndex(
                name: "IX_Deck_InstallationId",
                table: "Decks",
                newName: "IX_Decks_InstallationId");

            migrationBuilder.RenameIndex(
                name: "IX_Area_PlantId",
                table: "Areas",
                newName: "IX_Areas_PlantId");

            migrationBuilder.RenameIndex(
                name: "IX_Area_InstallationId",
                table: "Areas",
                newName: "IX_Areas_InstallationId");

            migrationBuilder.RenameIndex(
                name: "IX_Area_DeckId",
                table: "Areas",
                newName: "IX_Areas_DeckId");

            migrationBuilder.AddColumn<float>(
                name: "InspectionTarget_X",
                table: "MissionTask",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "InspectionTarget_Y",
                table: "MissionTask",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "InspectionTarget_Z",
                table: "MissionTask",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RobotPose_Orientation_W",
                table: "MissionTask",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RobotPose_Orientation_X",
                table: "MissionTask",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RobotPose_Orientation_Y",
                table: "MissionTask",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RobotPose_Orientation_Z",
                table: "MissionTask",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RobotPose_Position_X",
                table: "MissionTask",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RobotPose_Position_Y",
                table: "MissionTask",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RobotPose_Position_Z",
                table: "MissionTask",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_W",
                table: "SafePositions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_X",
                table: "SafePositions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_Y",
                table: "SafePositions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_Z",
                table: "SafePositions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Position_X",
                table: "SafePositions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Position_Y",
                table: "SafePositions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Position_Z",
                table: "SafePositions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_W",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_X",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_Y",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_Z",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Position_X",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Position_Y",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Position_Z",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_X1",
                table: "MissionRuns",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_X2",
                table: "MissionRuns",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Y1",
                table: "MissionRuns",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Y2",
                table: "MissionRuns",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Z1",
                table: "MissionRuns",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_Boundary_Z2",
                table: "MissionRuns",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Map_MapName",
                table: "MissionRuns",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_C1",
                table: "MissionRuns",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_C2",
                table: "MissionRuns",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_D1",
                table: "MissionRuns",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Map_TransformationMatrices_D2",
                table: "MissionRuns",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_W",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_X",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_Y",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_Z",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Position_X",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Position_Y",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Position_Z",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<double>(
                name: "MapMetadata_Boundary_X1",
                table: "Areas",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MapMetadata_Boundary_X2",
                table: "Areas",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MapMetadata_Boundary_Y1",
                table: "Areas",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MapMetadata_Boundary_Y2",
                table: "Areas",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MapMetadata_Boundary_Z1",
                table: "Areas",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MapMetadata_Boundary_Z2",
                table: "Areas",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "MapMetadata_MapName",
                table: "Areas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "MapMetadata_TransformationMatrices_C1",
                table: "Areas",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MapMetadata_TransformationMatrices_C2",
                table: "Areas",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MapMetadata_TransformationMatrices_D1",
                table: "Areas",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MapMetadata_TransformationMatrices_D2",
                table: "Areas",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sources",
                table: "Sources",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SafePositions",
                table: "SafePositions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RobotModels",
                table: "RobotModels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Robots",
                table: "Robots",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Plants",
                table: "Plants",
                column: "Id");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Missions",
                table: "MissionRuns");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MissionRuns",
                table: "MissionRuns",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MissionDefinitions",
                table: "MissionDefinitions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Installations",
                table: "Installations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Decks",
                table: "Decks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Areas",
                table: "Areas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Decks_DeckId",
                table: "Areas",
                column: "DeckId",
                principalTable: "Decks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Installations_InstallationId",
                table: "Areas",
                column: "InstallationId",
                principalTable: "Installations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Plants_PlantId",
                table: "Areas",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_MissionDefinitions_Areas_AreaId",
                table: "MissionDefinitions",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_MissionRuns_LastRunId",
                table: "MissionDefinitions",
                column: "LastRunId",
                principalTable: "MissionRuns",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_Sources_SourceId",
                table: "MissionDefinitions",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRuns_Areas_AreaId",
                table: "MissionRuns",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRuns_Robots_RobotId",
                table: "MissionRuns",
                column: "RobotId",
                principalTable: "Robots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTask_MissionRuns_MissionRunId",
                table: "MissionTask",
                column: "MissionRunId",
                principalTable: "MissionRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Plants_Installations_InstallationId",
                table: "Plants",
                column: "InstallationId",
                principalTable: "Installations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Areas_CurrentAreaId",
                table: "Robots",
                column: "CurrentAreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_RobotModels_ModelId",
                table: "Robots",
                column: "ModelId",
                principalTable: "RobotModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SafePositions_Areas_AreaId",
                table: "SafePositions",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoStream_Robots_RobotId",
                table: "VideoStream",
                column: "RobotId",
                principalTable: "Robots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Decks_DeckId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Installations_InstallationId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Plants_PlantId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_Installations_InstallationId",
                table: "Decks");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_Plants_PlantId",
                table: "Decks");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_Areas_AreaId",
                table: "MissionDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_MissionRuns_LastRunId",
                table: "MissionDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_Sources_SourceId",
                table: "MissionDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_Areas_AreaId",
                table: "MissionRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_Robots_RobotId",
                table: "MissionRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionTask_MissionRuns_MissionRunId",
                table: "MissionTask");

            migrationBuilder.DropForeignKey(
                name: "FK_Plants_Installations_InstallationId",
                table: "Plants");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Areas_CurrentAreaId",
                table: "Robots");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_RobotModels_ModelId",
                table: "Robots");

            migrationBuilder.DropForeignKey(
                name: "FK_SafePositions_Areas_AreaId",
                table: "SafePositions");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoStream_Robots_RobotId",
                table: "VideoStream");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sources",
                table: "Sources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SafePositions",
                table: "SafePositions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Robots",
                table: "Robots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RobotModels",
                table: "RobotModels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Plants",
                table: "Plants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MissionRuns",
                table: "MissionRuns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MissionDefinitions",
                table: "MissionDefinitions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Installations",
                table: "Installations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Decks",
                table: "Decks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Areas",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "InspectionTarget_X",
                table: "MissionTask");

            migrationBuilder.DropColumn(
                name: "InspectionTarget_Y",
                table: "MissionTask");

            migrationBuilder.DropColumn(
                name: "InspectionTarget_Z",
                table: "MissionTask");

            migrationBuilder.DropColumn(
                name: "RobotPose_Orientation_W",
                table: "MissionTask");

            migrationBuilder.DropColumn(
                name: "RobotPose_Orientation_X",
                table: "MissionTask");

            migrationBuilder.DropColumn(
                name: "RobotPose_Orientation_Y",
                table: "MissionTask");

            migrationBuilder.DropColumn(
                name: "RobotPose_Orientation_Z",
                table: "MissionTask");

            migrationBuilder.DropColumn(
                name: "RobotPose_Position_X",
                table: "MissionTask");

            migrationBuilder.DropColumn(
                name: "RobotPose_Position_Y",
                table: "MissionTask");

            migrationBuilder.DropColumn(
                name: "RobotPose_Position_Z",
                table: "MissionTask");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_W",
                table: "SafePositions");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_X",
                table: "SafePositions");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_Y",
                table: "SafePositions");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_Z",
                table: "SafePositions");

            migrationBuilder.DropColumn(
                name: "Pose_Position_X",
                table: "SafePositions");

            migrationBuilder.DropColumn(
                name: "Pose_Position_Y",
                table: "SafePositions");

            migrationBuilder.DropColumn(
                name: "Pose_Position_Z",
                table: "SafePositions");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_W",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_X",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_Y",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_Z",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Position_X",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Position_Y",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Position_Z",
                table: "Robots");

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
                name: "DefaultLocalizationPose_Orientation_W",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Orientation_X",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Orientation_Y",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Orientation_Z",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Position_X",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Position_Y",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Position_Z",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_Boundary_X1",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_Boundary_X2",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_Boundary_Y1",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_Boundary_Y2",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_Boundary_Z1",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_Boundary_Z2",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_MapName",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_TransformationMatrices_C1",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_TransformationMatrices_C2",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_TransformationMatrices_D1",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "MapMetadata_TransformationMatrices_D2",
                table: "Areas");

            migrationBuilder.RenameTable(
                name: "Sources",
                newName: "Source");

            migrationBuilder.RenameTable(
                name: "SafePositions",
                newName: "SafePosition");

            migrationBuilder.RenameTable(
                name: "Robots",
                newName: "Robot");

            migrationBuilder.RenameTable(
                name: "RobotModels",
                newName: "RobotModel");

            migrationBuilder.RenameTable(
                name: "Plants",
                newName: "Plant");

            migrationBuilder.RenameTable(
                name: "MissionRuns",
                newName: "MissionRun");

            migrationBuilder.RenameTable(
                name: "MissionDefinitions",
                newName: "MissionDefinition");

            migrationBuilder.RenameTable(
                name: "Installations",
                newName: "Installation");

            migrationBuilder.RenameTable(
                name: "Decks",
                newName: "Deck");

            migrationBuilder.RenameTable(
                name: "Areas",
                newName: "Area");

            migrationBuilder.RenameIndex(
                name: "IX_SafePositions_AreaId",
                table: "SafePosition",
                newName: "IX_SafePosition_AreaId");

            migrationBuilder.RenameIndex(
                name: "IX_Robots_ModelId",
                table: "Robot",
                newName: "IX_Robot_ModelId");

            migrationBuilder.RenameIndex(
                name: "IX_Robots_CurrentAreaId",
                table: "Robot",
                newName: "IX_Robot_CurrentAreaId");

            migrationBuilder.RenameIndex(
                name: "IX_RobotModels_Type",
                table: "RobotModel",
                newName: "IX_RobotModel_Type");

            migrationBuilder.RenameIndex(
                name: "IX_Plants_PlantCode",
                table: "Plant",
                newName: "IX_Plant_PlantCode");

            migrationBuilder.RenameIndex(
                name: "IX_Plants_InstallationId",
                table: "Plant",
                newName: "IX_Plant_InstallationId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionRuns_RobotId",
                table: "MissionRun",
                newName: "IX_MissionRun_RobotId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionRuns_AreaId",
                table: "MissionRun",
                newName: "IX_MissionRun_AreaId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionDefinitions_SourceId",
                table: "MissionDefinition",
                newName: "IX_MissionDefinition_SourceId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionDefinitions_LastRunId",
                table: "MissionDefinition",
                newName: "IX_MissionDefinition_LastRunId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionDefinitions_AreaId",
                table: "MissionDefinition",
                newName: "IX_MissionDefinition_AreaId");

            migrationBuilder.RenameIndex(
                name: "IX_Installations_InstallationCode",
                table: "Installation",
                newName: "IX_Installation_InstallationCode");

            migrationBuilder.RenameIndex(
                name: "IX_Decks_PlantId",
                table: "Deck",
                newName: "IX_Deck_PlantId");

            migrationBuilder.RenameIndex(
                name: "IX_Decks_InstallationId",
                table: "Deck",
                newName: "IX_Deck_InstallationId");

            migrationBuilder.RenameIndex(
                name: "IX_Areas_PlantId",
                table: "Area",
                newName: "IX_Area_PlantId");

            migrationBuilder.RenameIndex(
                name: "IX_Areas_InstallationId",
                table: "Area",
                newName: "IX_Area_InstallationId");

            migrationBuilder.RenameIndex(
                name: "IX_Areas_DeckId",
                table: "Area",
                newName: "IX_Area_DeckId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Source",
                table: "Source",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SafePosition",
                table: "SafePosition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Robot",
                table: "Robot",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RobotModel",
                table: "RobotModel",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Plant",
                table: "Plant",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MissionRun",
                table: "MissionRun",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MissionDefinition",
                table: "MissionDefinition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Installation",
                table: "Installation",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Deck",
                table: "Deck",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Area",
                table: "Area",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Area.DefaultLocalizationPose#Pose",
                columns: table => new
                {
                    AreaId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Area.DefaultLocalizationPose#Pose", x => x.AreaId);
                    table.ForeignKey(
                        name: "FK_Area.DefaultLocalizationPose#Pose_Area_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Area",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Area.MapMetadata#MapMetadata",
                columns: table => new
                {
                    AreaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MapName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Area.MapMetadata#MapMetadata", x => x.AreaId);
                    table.ForeignKey(
                        name: "FK_Area.MapMetadata#MapMetadata_Area_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Area",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetDeck",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeckName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDeck", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MissionRun.MapMetadata#MapMetadata",
                columns: table => new
                {
                    MissionRunId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MapName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionRun.MapMetadata#MapMetadata", x => x.MissionRunId);
                    table.ForeignKey(
                        name: "FK_MissionRun.MapMetadata#MapMetadata_MissionRun_MissionRunId",
                        column: x => x.MissionRunId,
                        principalTable: "MissionRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionTask.InspectionTarget#Position",
                columns: table => new
                {
                    MissionTaskId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTask.InspectionTarget#Position", x => x.MissionTaskId);
                    table.ForeignKey(
                        name: "FK_MissionTask.InspectionTarget#Position_MissionTask_MissionTaskId",
                        column: x => x.MissionTaskId,
                        principalTable: "MissionTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionTask.RobotPose#Pose",
                columns: table => new
                {
                    MissionTaskId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTask.RobotPose#Pose", x => x.MissionTaskId);
                    table.ForeignKey(
                        name: "FK_MissionTask.RobotPose#Pose_MissionTask_MissionTaskId",
                        column: x => x.MissionTaskId,
                        principalTable: "MissionTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Robot.Pose#Pose",
                columns: table => new
                {
                    RobotId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Robot.Pose#Pose", x => x.RobotId);
                    table.ForeignKey(
                        name: "FK_Robot.Pose#Pose_Robot_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafePosition.Pose#Pose",
                columns: table => new
                {
                    SafePositionId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafePosition.Pose#Pose", x => x.SafePositionId);
                    table.ForeignKey(
                        name: "FK_SafePosition.Pose#Pose_SafePosition_SafePositionId",
                        column: x => x.SafePositionId,
                        principalTable: "SafePosition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Area.DefaultLocalizationPose#Pose.Orientation#Orientation",
                columns: table => new
                {
                    PoseAreaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    W = table.Column<float>(type: "real", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Area.DefaultLocalizationPose#Pose.Orientation#Orientation", x => x.PoseAreaId);
                    table.ForeignKey(
                        name: "FK_Area.DefaultLocalizationPose#Pose.Orientation#Orientation_Area.DefaultLocalizationPose#Pose_PoseAreaId",
                        column: x => x.PoseAreaId,
                        principalTable: "Area.DefaultLocalizationPose#Pose",
                        principalColumn: "AreaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Area.DefaultLocalizationPose#Pose.Position#Position",
                columns: table => new
                {
                    PoseAreaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Area.DefaultLocalizationPose#Pose.Position#Position", x => x.PoseAreaId);
                    table.ForeignKey(
                        name: "FK_Area.DefaultLocalizationPose#Pose.Position#Position_Area.DefaultLocalizationPose#Pose_PoseAreaId",
                        column: x => x.PoseAreaId,
                        principalTable: "Area.DefaultLocalizationPose#Pose",
                        principalColumn: "AreaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Area.MapMetadata#MapMetadata.Boundary#Boundary",
                columns: table => new
                {
                    MapMetadataAreaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X1 = table.Column<double>(type: "float", nullable: false),
                    X2 = table.Column<double>(type: "float", nullable: false),
                    Y1 = table.Column<double>(type: "float", nullable: false),
                    Y2 = table.Column<double>(type: "float", nullable: false),
                    Z1 = table.Column<double>(type: "float", nullable: false),
                    Z2 = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Area.MapMetadata#MapMetadata.Boundary#Boundary", x => x.MapMetadataAreaId);
                    table.ForeignKey(
                        name: "FK_Area.MapMetadata#MapMetadata.Boundary#Boundary_Area.MapMetadata#MapMetadata_MapMetadataAreaId",
                        column: x => x.MapMetadataAreaId,
                        principalTable: "Area.MapMetadata#MapMetadata",
                        principalColumn: "AreaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Area.MapMetadata#MapMetadata.TransformationMatrices#TransformationMatrices",
                columns: table => new
                {
                    MapMetadataAreaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    C1 = table.Column<double>(type: "float", nullable: false),
                    C2 = table.Column<double>(type: "float", nullable: false),
                    D1 = table.Column<double>(type: "float", nullable: false),
                    D2 = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Area.MapMetadata#MapMetadata.TransformationMatrices#TransformationMatrices", x => x.MapMetadataAreaId);
                    table.ForeignKey(
                        name: "FK_Area.MapMetadata#MapMetadata.TransformationMatrices#TransformationMatrices_Area.MapMetadata#MapMetadata_MapMetadataAreaId",
                        column: x => x.MapMetadataAreaId,
                        principalTable: "Area.MapMetadata#MapMetadata",
                        principalColumn: "AreaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionRun.MapMetadata#MapMetadata.Boundary#Boundary",
                columns: table => new
                {
                    MapMetadataMissionRunId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X1 = table.Column<double>(type: "float", nullable: false),
                    X2 = table.Column<double>(type: "float", nullable: false),
                    Y1 = table.Column<double>(type: "float", nullable: false),
                    Y2 = table.Column<double>(type: "float", nullable: false),
                    Z1 = table.Column<double>(type: "float", nullable: false),
                    Z2 = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionRun.MapMetadata#MapMetadata.Boundary#Boundary", x => x.MapMetadataMissionRunId);
                    table.ForeignKey(
                        name: "FK_MissionRun.MapMetadata#MapMetadata.Boundary#Boundary_MissionRun.MapMetadata#MapMetadata_MapMetadataMissionRunId",
                        column: x => x.MapMetadataMissionRunId,
                        principalTable: "MissionRun.MapMetadata#MapMetadata",
                        principalColumn: "MissionRunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionRun.MapMetadata#MapMetadata.TransformationMatrices#TransformationMatrices",
                columns: table => new
                {
                    MapMetadataMissionRunId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    C1 = table.Column<double>(type: "float", nullable: false),
                    C2 = table.Column<double>(type: "float", nullable: false),
                    D1 = table.Column<double>(type: "float", nullable: false),
                    D2 = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionRun.MapMetadata#MapMetadata.TransformationMatrices#TransformationMatrices", x => x.MapMetadataMissionRunId);
                    table.ForeignKey(
                        name: "FK_MissionRun.MapMetadata#MapMetadata.TransformationMatrices#TransformationMatrices_MissionRun.MapMetadata#MapMetadata_MapMetad~",
                        column: x => x.MapMetadataMissionRunId,
                        principalTable: "MissionRun.MapMetadata#MapMetadata",
                        principalColumn: "MissionRunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionTask.RobotPose#Pose.Orientation#Orientation",
                columns: table => new
                {
                    PoseMissionTaskId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    W = table.Column<float>(type: "real", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTask.RobotPose#Pose.Orientation#Orientation", x => x.PoseMissionTaskId);
                    table.ForeignKey(
                        name: "FK_MissionTask.RobotPose#Pose.Orientation#Orientation_MissionTask.RobotPose#Pose_PoseMissionTaskId",
                        column: x => x.PoseMissionTaskId,
                        principalTable: "MissionTask.RobotPose#Pose",
                        principalColumn: "MissionTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionTask.RobotPose#Pose.Position#Position",
                columns: table => new
                {
                    PoseMissionTaskId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTask.RobotPose#Pose.Position#Position", x => x.PoseMissionTaskId);
                    table.ForeignKey(
                        name: "FK_MissionTask.RobotPose#Pose.Position#Position_MissionTask.RobotPose#Pose_PoseMissionTaskId",
                        column: x => x.PoseMissionTaskId,
                        principalTable: "MissionTask.RobotPose#Pose",
                        principalColumn: "MissionTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Robot.Pose#Pose.Orientation#Orientation",
                columns: table => new
                {
                    PoseRobotId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    W = table.Column<float>(type: "real", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Robot.Pose#Pose.Orientation#Orientation", x => x.PoseRobotId);
                    table.ForeignKey(
                        name: "FK_Robot.Pose#Pose.Orientation#Orientation_Robot.Pose#Pose_PoseRobotId",
                        column: x => x.PoseRobotId,
                        principalTable: "Robot.Pose#Pose",
                        principalColumn: "RobotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Robot.Pose#Pose.Position#Position",
                columns: table => new
                {
                    PoseRobotId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Robot.Pose#Pose.Position#Position", x => x.PoseRobotId);
                    table.ForeignKey(
                        name: "FK_Robot.Pose#Pose.Position#Position_Robot.Pose#Pose_PoseRobotId",
                        column: x => x.PoseRobotId,
                        principalTable: "Robot.Pose#Pose",
                        principalColumn: "RobotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafePosition.Pose#Pose.Orientation#Orientation",
                columns: table => new
                {
                    PoseSafePositionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    W = table.Column<float>(type: "real", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafePosition.Pose#Pose.Orientation#Orientation", x => x.PoseSafePositionId);
                    table.ForeignKey(
                        name: "FK_SafePosition.Pose#Pose.Orientation#Orientation_SafePosition.Pose#Pose_PoseSafePositionId",
                        column: x => x.PoseSafePositionId,
                        principalTable: "SafePosition.Pose#Pose",
                        principalColumn: "SafePositionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafePosition.Pose#Pose.Position#Position",
                columns: table => new
                {
                    PoseSafePositionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafePosition.Pose#Pose.Position#Position", x => x.PoseSafePositionId);
                    table.ForeignKey(
                        name: "FK_SafePosition.Pose#Pose.Position#Position_SafePosition.Pose#Pose_PoseSafePositionId",
                        column: x => x.PoseSafePositionId,
                        principalTable: "SafePosition.Pose#Pose",
                        principalColumn: "SafePositionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Area_Deck_DeckId",
                table: "Area",
                column: "DeckId",
                principalTable: "Deck",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Area_Installation_InstallationId",
                table: "Area",
                column: "InstallationId",
                principalTable: "Installation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Area_Plant_PlantId",
                table: "Area",
                column: "PlantId",
                principalTable: "Plant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Deck_Installation_InstallationId",
                table: "Deck",
                column: "InstallationId",
                principalTable: "Installation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Deck_Plant_PlantId",
                table: "Deck",
                column: "PlantId",
                principalTable: "Plant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinition_Area_AreaId",
                table: "MissionDefinition",
                column: "AreaId",
                principalTable: "Area",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinition_MissionRun_LastRunId",
                table: "MissionDefinition",
                column: "LastRunId",
                principalTable: "MissionRun",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinition_Source_SourceId",
                table: "MissionDefinition",
                column: "SourceId",
                principalTable: "Source",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRun_Area_AreaId",
                table: "MissionRun",
                column: "AreaId",
                principalTable: "Area",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRun_Robot_RobotId",
                table: "MissionRun",
                column: "RobotId",
                principalTable: "Robot",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTask_MissionRun_MissionRunId",
                table: "MissionTask",
                column: "MissionRunId",
                principalTable: "MissionRun",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Plant_Installation_InstallationId",
                table: "Plant",
                column: "InstallationId",
                principalTable: "Installation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Robot_Area_CurrentAreaId",
                table: "Robot",
                column: "CurrentAreaId",
                principalTable: "Area",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Robot_RobotModel_ModelId",
                table: "Robot",
                column: "ModelId",
                principalTable: "RobotModel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SafePosition_Area_AreaId",
                table: "SafePosition",
                column: "AreaId",
                principalTable: "Area",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoStream_Robot_RobotId",
                table: "VideoStream",
                column: "RobotId",
                principalTable: "Robot",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
