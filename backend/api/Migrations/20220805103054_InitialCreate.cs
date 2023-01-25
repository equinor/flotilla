using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orientations",
                columns: table =>
                    new
                    {
                        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        X = table.Column<float>(type: "real", nullable: false),
                        Y = table.Column<float>(type: "real", nullable: false),
                        Z = table.Column<float>(type: "real", nullable: false),
                        W = table.Column<float>(type: "real", nullable: false)
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orientations", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table =>
                    new
                    {
                        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        X = table.Column<float>(type: "real", nullable: false),
                        Y = table.Column<float>(type: "real", nullable: false),
                        Z = table.Column<float>(type: "real", nullable: false)
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Poses",
                columns: table =>
                    new
                    {
                        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        PositionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                        OrientationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                        Frame = table.Column<string>(type: "nvarchar(max)", nullable: true)
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Poses_Orientations_OrientationId",
                        column: x => x.OrientationId,
                        principalTable: "Orientations",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Poses_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Robots",
                columns: table =>
                    new
                    {
                        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        Name = table.Column<string>(
                            type: "nvarchar(128)",
                            maxLength: 128,
                            nullable: false
                        ),
                        Model = table.Column<string>(
                            type: "nvarchar(128)",
                            maxLength: 128,
                            nullable: false
                        ),
                        SerialNumber = table.Column<string>(
                            type: "nvarchar(128)",
                            maxLength: 128,
                            nullable: false
                        ),
                        Logs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        BatteryLevel = table.Column<float>(type: "real", nullable: false),
                        Host = table.Column<string>(
                            type: "nvarchar(128)",
                            maxLength: 128,
                            nullable: false
                        ),
                        Port = table.Column<int>(type: "int", nullable: false),
                        Enabled = table.Column<bool>(type: "bit", nullable: false),
                        Status = table.Column<int>(type: "int", nullable: false),
                        PoseId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Robots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Robots_Poses_PoseId",
                        column: x => x.PoseId,
                        principalTable: "Poses",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table =>
                    new
                    {
                        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        AssetCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        RobotId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        IsarMissionId = table.Column<string>(
                            type: "nvarchar(128)",
                            maxLength: 128,
                            nullable: false
                        ),
                        EchoMissionId = table.Column<int>(
                            type: "int",
                            maxLength: 128,
                            nullable: false
                        ),
                        Log = table.Column<string>(
                            type: "nvarchar(128)",
                            maxLength: 128,
                            nullable: true
                        ),
                        ReportStatus = table.Column<int>(type: "int", nullable: false),
                        StartTime = table.Column<DateTimeOffset>(
                            type: "datetimeoffset",
                            nullable: false
                        ),
                        EndTime = table.Column<DateTimeOffset>(
                            type: "datetimeoffset",
                            nullable: false
                        )
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ScheduledMissions",
                columns: table =>
                    new
                    {
                        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        RobotId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        EchoMissionId = table.Column<int>(
                            type: "int",
                            maxLength: 128,
                            nullable: false
                        ),
                        StartTime = table.Column<DateTimeOffset>(
                            type: "datetimeoffset",
                            nullable: false
                        ),
                        EndTime = table.Column<DateTimeOffset>(
                            type: "datetimeoffset",
                            nullable: false
                        ),
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
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "VideoStreams",
                columns: table =>
                    new
                    {
                        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        RobotId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                        Name = table.Column<string>(
                            type: "nvarchar(64)",
                            maxLength: 64,
                            nullable: false
                        ),
                        Url = table.Column<string>(
                            type: "nvarchar(128)",
                            maxLength: 128,
                            nullable: false
                        )
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoStreams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoStreams_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table =>
                    new
                    {
                        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        IsarTaskId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        ReportId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        TagId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        TaskStatus = table.Column<int>(type: "int", nullable: false),
                        Time = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Steps",
                columns: table =>
                    new
                    {
                        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        IsarStepId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        TaskId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        TagId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        StepStatus = table.Column<int>(type: "int", nullable: false),
                        StepType = table.Column<int>(type: "int", nullable: false),
                        InspectionType = table.Column<int>(type: "int", nullable: false),
                        Time = table.Column<DateTimeOffset>(
                            type: "datetimeoffset",
                            nullable: false
                        ),
                        FileLocation = table.Column<string>(
                            type: "nvarchar(128)",
                            maxLength: 128,
                            nullable: true
                        )
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Steps_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Poses_OrientationId",
                table: "Poses",
                column: "OrientationId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Poses_PositionId",
                table: "Poses",
                column: "PositionId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Reports_RobotId",
                table: "Reports",
                column: "RobotId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Robots_PoseId",
                table: "Robots",
                column: "PoseId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMissions_RobotId",
                table: "ScheduledMissions",
                column: "RobotId"
            );

            migrationBuilder.CreateIndex(name: "IX_Steps_TaskId", table: "Steps", column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ReportId",
                table: "Tasks",
                column: "ReportId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_VideoStreams_RobotId",
                table: "VideoStreams",
                column: "RobotId"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ScheduledMissions");

            migrationBuilder.DropTable(name: "Steps");

            migrationBuilder.DropTable(name: "VideoStreams");

            migrationBuilder.DropTable(name: "Tasks");

            migrationBuilder.DropTable(name: "Reports");

            migrationBuilder.DropTable(name: "Robots");

            migrationBuilder.DropTable(name: "Poses");

            migrationBuilder.DropTable(name: "Orientations");

            migrationBuilder.DropTable(name: "Positions");
        }
    }
}
