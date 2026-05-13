using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskDefinitionRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_Sources_SourceId",
                table: "MissionDefinitions");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropIndex(
                name: "IX_MissionDefinitions_SourceId",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "PoseId",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "TagLink",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "MissionDefinitions");

            migrationBuilder.RenameColumn(
                name: "InspectionTargetName",
                table: "Inspections",
                newName: "TaskDescription");

            migrationBuilder.AddColumn<string>(
                name: "AnalysisTypes",
                table: "MissionTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnalysisTypes",
                table: "Inspections",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskDefinition",
                columns: table => new
                {
                    Index = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MissionDefinitionId = table.Column<string>(type: "text", nullable: false),
                    TagId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RobotPose_Position_X = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Position_Z = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    TargetPosition_X = table.Column<float>(type: "real", nullable: false),
                    TargetPosition_Y = table.Column<float>(type: "real", nullable: false),
                    TargetPosition_Z = table.Column<float>(type: "real", nullable: false),
                    ZoomDescription_ObjectWidth = table.Column<double>(type: "double precision", nullable: true),
                    ZoomDescription_ObjectHeight = table.Column<double>(type: "double precision", nullable: true),
                    AnalysisTypes = table.Column<string>(type: "text", nullable: false),
                    SensorType = table.Column<string>(type: "text", nullable: false),
                    VideoDuration = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDefinition", x => new { x.MissionDefinitionId, x.Index });
                    table.ForeignKey(
                        name: "FK_TaskDefinition_MissionDefinitions_MissionDefinitionId",
                        column: x => x.MissionDefinitionId,
                        principalTable: "MissionDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "AnalysisTypes",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "AnalysisTypes",
                table: "Inspections");

            migrationBuilder.RenameColumn(
                name: "TaskDescription",
                table: "Inspections",
                newName: "InspectionTargetName");

            migrationBuilder.AddColumn<int>(
                name: "PoseId",
                table: "MissionTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagLink",
                table: "MissionTasks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "MissionDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CustomMissionTasks = table.Column<string>(type: "text", nullable: true),
                    SourceId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinitions_SourceId",
                table: "MissionDefinitions",
                column: "SourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_Sources_SourceId",
                table: "MissionDefinitions",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
