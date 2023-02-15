using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Robots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Model = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Logs = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BatteryLevel = table.Column<float>(type: "real", nullable: false),
                    Host = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Pose_Position_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    Pose_Frame = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Robots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StatusReason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssetCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RobotId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsarMissionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EchoMissionId = table.Column<int>(type: "int", maxLength: 200, nullable: false),
                    MissionStatus = table.Column<int>(type: "int", nullable: false),
                    Map_MapName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Map_Boundary_X1 = table.Column<double>(type: "float", nullable: false),
                    Map_Boundary_Y1 = table.Column<double>(type: "float", nullable: false),
                    Map_Boundary_X2 = table.Column<double>(type: "float", nullable: false),
                    Map_Boundary_Y2 = table.Column<double>(type: "float", nullable: false),
                    Map_TransformationMatrices_C1 = table.Column<double>(type: "float", nullable: false),
                    Map_TransformationMatrices_C2 = table.Column<double>(type: "float", nullable: false),
                    Map_TransformationMatrices_D1 = table.Column<double>(type: "float", nullable: false),
                    Map_TransformationMatrices_D2 = table.Column<double>(type: "float", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EstimatedDuration = table.Column<TimeSpan>(type: "time", nullable: false)
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
                name: "VideoStream",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RobotId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoStream", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoStream_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IsarTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsarTaskId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MissionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TagId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EchoLink = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaskStatus = table.Column<int>(type: "int", nullable: false),
                    Time = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IsarTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsarTask_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TagId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    URL = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TagPosition_X = table.Column<float>(type: "real", nullable: false),
                    TagPosition_Y = table.Column<float>(type: "real", nullable: false),
                    TagPosition_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    Pose_Frame = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PoseId = table.Column<int>(type: "int", nullable: false),
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
                name: "IsarStep",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsarStepId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaskId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TagId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StepStatus = table.Column<int>(type: "int", nullable: false),
                    StepType = table.Column<int>(type: "int", nullable: false),
                    InspectionType = table.Column<int>(type: "int", nullable: false),
                    Time = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FileLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IsarStep", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsarStep_IsarTask_TaskId",
                        column: x => x.TaskId,
                        principalTable: "IsarTask",
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
                name: "IX_IsarStep_TaskId",
                table: "IsarStep",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_IsarTask_MissionId",
                table: "IsarTask",
                column: "MissionId");

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

            migrationBuilder.CreateIndex(
                name: "IX_VideoStream_RobotId",
                table: "VideoStream",
                column: "RobotId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IsarStep");

            migrationBuilder.DropTable(
                name: "PlannedInspection");

            migrationBuilder.DropTable(
                name: "VideoStream");

            migrationBuilder.DropTable(
                name: "IsarTask");

            migrationBuilder.DropTable(
                name: "PlannedTask");

            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.DropTable(
                name: "Robots");
        }
    }
}
