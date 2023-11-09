using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeMissionTaskNotOwned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inspections_MissionTask_MissionTaskId",
                table: "Inspections");

            migrationBuilder.DropTable(
                name: "MissionTask");

            migrationBuilder.CreateTable(
                name: "MissionTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IsarTaskId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TaskOrder = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EchoTagLink = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InspectionTarget_X = table.Column<float>(type: "real", nullable: false),
                    InspectionTarget_Y = table.Column<float>(type: "real", nullable: false),
                    InspectionTarget_Z = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Position_X = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Position_Z = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    EchoPoseId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MissionRunId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionTasks_MissionRuns_MissionRunId",
                        column: x => x.MissionRunId,
                        principalTable: "MissionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionTasks_MissionRunId",
                table: "MissionTasks",
                column: "MissionRunId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inspections_MissionTasks_MissionTaskId",
                table: "Inspections",
                column: "MissionTaskId",
                principalTable: "MissionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inspections_MissionTasks_MissionTaskId",
                table: "Inspections");

            migrationBuilder.DropTable(
                name: "MissionTasks");

            migrationBuilder.CreateTable(
                name: "MissionTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EchoPoseId = table.Column<int>(type: "integer", nullable: true),
                    EchoTagLink = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsarTaskId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MissionRunId = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TagId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TaskOrder = table.Column<int>(type: "integer", nullable: false),
                    InspectionTarget_X = table.Column<float>(type: "real", nullable: false),
                    InspectionTarget_Y = table.Column<float>(type: "real", nullable: false),
                    InspectionTarget_Z = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Position_X = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Position_Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionTask_MissionRuns_MissionRunId",
                        column: x => x.MissionRunId,
                        principalTable: "MissionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionTask_MissionRunId",
                table: "MissionTask",
                column: "MissionRunId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inspections_MissionTask_MissionTaskId",
                table: "Inspections",
                column: "MissionTaskId",
                principalTable: "MissionTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
