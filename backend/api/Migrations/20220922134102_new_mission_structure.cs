using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class new_mission_structure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Poses_PoseId",
                table: "Robots");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Tasks_TaskId",
                table: "Steps");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Reports_ReportId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoStreams_Robots_RobotId",
                table: "VideoStreams");

            migrationBuilder.DropTable(
                name: "Poses");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "ScheduledMissions");

            migrationBuilder.DropTable(
                name: "Orientations");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Robots_PoseId",
                table: "Robots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoStreams",
                table: "VideoStreams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Steps",
                table: "Steps");

            migrationBuilder.DropColumn(
                name: "PoseId",
                table: "Robots");

            migrationBuilder.RenameTable(
                name: "VideoStreams",
                newName: "VideoStream");

            migrationBuilder.RenameTable(
                name: "Tasks",
                newName: "IsarTask");

            migrationBuilder.RenameTable(
                name: "Steps",
                newName: "IsarStep");

            migrationBuilder.RenameIndex(
                name: "IX_VideoStreams_RobotId",
                table: "VideoStream",
                newName: "IX_VideoStream_RobotId");

            migrationBuilder.RenameColumn(
                name: "ReportId",
                table: "IsarTask",
                newName: "MissionId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_ReportId",
                table: "IsarTask",
                newName: "IX_IsarTask_MissionId");

            migrationBuilder.RenameIndex(
                name: "IX_Steps_TaskId",
                table: "IsarStep",
                newName: "IX_IsarStep_TaskId");

            migrationBuilder.AddColumn<string>(
                name: "Pose_Frame",
                table: "Robots",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

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

            migrationBuilder.AlterColumn<string>(
                name: "RobotId",
                table: "VideoStream",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoStream",
                table: "VideoStream",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IsarTask",
                table: "IsarTask",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IsarStep",
                table: "IsarStep",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RobotId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsarMissionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EchoMissionId = table.Column<int>(type: "int", maxLength: 128, nullable: false),
                    MissionStatus = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Missions_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TagId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    URL = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MissionId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedTask_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedInspection",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InspectionType = table.Column<int>(type: "int", nullable: false),
                    TimeInSeconds = table.Column<float>(type: "real", nullable: true),
                    PlannedTaskId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedInspection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedInspection_PlannedTask_PlannedTaskId",
                        column: x => x.PlannedTaskId,
                        principalTable: "PlannedTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Missions_RobotId",
                table: "Missions",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedInspection_PlannedTaskId",
                table: "PlannedInspection",
                column: "PlannedTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedTask_MissionId",
                table: "PlannedTask",
                column: "MissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_IsarStep_IsarTask_TaskId",
                table: "IsarStep",
                column: "TaskId",
                principalTable: "IsarTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IsarTask_Missions_MissionId",
                table: "IsarTask",
                column: "MissionId",
                principalTable: "Missions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoStream_Robots_RobotId",
                table: "VideoStream",
                column: "RobotId",
                principalTable: "Robots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IsarStep_IsarTask_TaskId",
                table: "IsarStep");

            migrationBuilder.DropForeignKey(
                name: "FK_IsarTask_Missions_MissionId",
                table: "IsarTask");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoStream_Robots_RobotId",
                table: "VideoStream");

            migrationBuilder.DropTable(
                name: "PlannedInspection");

            migrationBuilder.DropTable(
                name: "PlannedTask");

            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoStream",
                table: "VideoStream");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IsarTask",
                table: "IsarTask");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IsarStep",
                table: "IsarStep");

            migrationBuilder.DropColumn(
                name: "Pose_Frame",
                table: "Robots");

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

            migrationBuilder.RenameTable(
                name: "VideoStream",
                newName: "VideoStreams");

            migrationBuilder.RenameTable(
                name: "IsarTask",
                newName: "Tasks");

            migrationBuilder.RenameTable(
                name: "IsarStep",
                newName: "Steps");

            migrationBuilder.RenameIndex(
                name: "IX_VideoStream_RobotId",
                table: "VideoStreams",
                newName: "IX_VideoStreams_RobotId");

            migrationBuilder.RenameColumn(
                name: "MissionId",
                table: "Tasks",
                newName: "ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_IsarTask_MissionId",
                table: "Tasks",
                newName: "IX_Tasks_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_IsarStep_TaskId",
                table: "Steps",
                newName: "IX_Steps_TaskId");

            migrationBuilder.AddColumn<string>(
                name: "PoseId",
                table: "Robots",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RobotId",
                table: "VideoStreams",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoStreams",
                table: "VideoStreams",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Steps",
                table: "Steps",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Orientations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    W = table.Column<float>(type: "real", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orientations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RobotId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EchoMissionId = table.Column<int>(type: "int", maxLength: 128, nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsarMissionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Log = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReportStatus = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledMissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RobotId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EchoMissionId = table.Column<int>(type: "int", maxLength: 128, nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledMissions_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Poses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrientationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PositionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Frame = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Poses_Orientations_OrientationId",
                        column: x => x.OrientationId,
                        principalTable: "Orientations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Poses_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Robots_PoseId",
                table: "Robots",
                column: "PoseId");

            migrationBuilder.CreateIndex(
                name: "IX_Poses_OrientationId",
                table: "Poses",
                column: "OrientationId");

            migrationBuilder.CreateIndex(
                name: "IX_Poses_PositionId",
                table: "Poses",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_RobotId",
                table: "Reports",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMissions_RobotId",
                table: "ScheduledMissions",
                column: "RobotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Poses_PoseId",
                table: "Robots",
                column: "PoseId",
                principalTable: "Poses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Tasks_TaskId",
                table: "Steps",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Reports_ReportId",
                table: "Tasks",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoStreams_Robots_RobotId",
                table: "VideoStreams",
                column: "RobotId",
                principalTable: "Robots",
                principalColumn: "Id");
        }
    }
}
