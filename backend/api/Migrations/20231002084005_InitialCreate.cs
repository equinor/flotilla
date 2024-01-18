using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Installations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InstallationCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RobotModels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    BatteryWarningThreshold = table.Column<float>(type: "real", nullable: true),
                    UpperPressureWarningThreshold = table.Column<float>(type: "real", nullable: true),
                    LowerPressureWarningThreshold = table.Column<float>(type: "real", nullable: true),
                    AverageDurationPerTag = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SourceId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    InstallationId = table.Column<string>(type: "text", nullable: false),
                    PlantCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plants_Installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Decks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PlantId = table.Column<string>(type: "text", nullable: false),
                    InstallationId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decks_Installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Decks_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DeckId = table.Column<string>(type: "text", nullable: true),
                    PlantId = table.Column<string>(type: "text", nullable: false),
                    InstallationId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MapMetadata_MapName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MapMetadata_Boundary_X1 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_Boundary_X2 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_Boundary_Y1 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_Boundary_Y2 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_Boundary_Z1 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_Boundary_Z2 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_TransformationMatrices_C1 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_TransformationMatrices_C2 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_TransformationMatrices_D1 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_TransformationMatrices_D2 = table.Column<double>(type: "double precision", nullable: false),
                    DefaultLocalizationPose_Position_X = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Position_Z = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Orientation_W = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Areas_Decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Areas_Installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Areas_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Robots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsarId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelId = table.Column<string>(type: "text", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentInstallation = table.Column<string>(type: "text", nullable: false),
                    CurrentAreaId = table.Column<string>(type: "text", nullable: true),
                    BatteryLevel = table.Column<float>(type: "real", nullable: false),
                    PressureLevel = table.Column<float>(type: "real", nullable: true),
                    Host = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Pose_Position_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    CurrentMissionId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Robots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Robots_Areas_CurrentAreaId",
                        column: x => x.CurrentAreaId,
                        principalTable: "Areas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Robots_RobotModels_ModelId",
                        column: x => x.ModelId,
                        principalTable: "RobotModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafePositions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Pose_Position_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    AreaId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafePositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafePositions_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MissionRuns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MissionId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    InstallationCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DesiredStartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    IsarMissionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    StatusReason = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AreaId = table.Column<string>(type: "text", nullable: true),
                    Map_MapName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Map_Boundary_X1 = table.Column<double>(type: "double precision", nullable: true),
                    Map_Boundary_X2 = table.Column<double>(type: "double precision", nullable: true),
                    Map_Boundary_Y1 = table.Column<double>(type: "double precision", nullable: true),
                    Map_Boundary_Y2 = table.Column<double>(type: "double precision", nullable: true),
                    Map_Boundary_Z1 = table.Column<double>(type: "double precision", nullable: true),
                    Map_Boundary_Z2 = table.Column<double>(type: "double precision", nullable: true),
                    Map_TransformationMatrices_C1 = table.Column<double>(type: "double precision", nullable: true),
                    Map_TransformationMatrices_C2 = table.Column<double>(type: "double precision", nullable: true),
                    Map_TransformationMatrices_D1 = table.Column<double>(type: "double precision", nullable: true),
                    Map_TransformationMatrices_D2 = table.Column<double>(type: "double precision", nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EstimatedDuration = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionRuns_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MissionRuns_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoStream",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ShouldRotate270Clockwise = table.Column<bool>(type: "boolean", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false)
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
                name: "MissionDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SourceId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InstallationCode = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InspectionFrequency = table.Column<long>(type: "bigint", nullable: true),
                    LastRunId = table.Column<string>(type: "text", nullable: true),
                    AreaId = table.Column<string>(type: "text", nullable: true),
                    IsDeprecated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionDefinitions_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MissionDefinitions_MissionRuns_LastRunId",
                        column: x => x.LastRunId,
                        principalTable: "MissionRuns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MissionDefinitions_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionTask",
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
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MissionRunId = table.Column<string>(type: "text", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Inspection",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IsarStepId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    InspectionType = table.Column<string>(type: "text", nullable: false),
                    VideoDuration = table.Column<float>(type: "real", nullable: true),
                    AnalysisType = table.Column<string>(type: "text", nullable: true),
                    InspectionUrl = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MissionTaskId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspection_MissionTask_MissionTaskId",
                        column: x => x.MissionTaskId,
                        principalTable: "MissionTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_DeckId",
                table: "Areas",
                column: "DeckId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_InstallationId",
                table: "Areas",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_PlantId",
                table: "Areas",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Decks_InstallationId",
                table: "Decks",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Decks_PlantId",
                table: "Decks",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_MissionTaskId",
                table: "Inspection",
                column: "MissionTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Installations_InstallationCode",
                table: "Installations",
                column: "InstallationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinitions_AreaId",
                table: "MissionDefinitions",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinitions_LastRunId",
                table: "MissionDefinitions",
                column: "LastRunId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinitions_SourceId",
                table: "MissionDefinitions",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionRuns_AreaId",
                table: "MissionRuns",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionRuns_RobotId",
                table: "MissionRuns",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTask_MissionRunId",
                table: "MissionTask",
                column: "MissionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_InstallationId",
                table: "Plants",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_PlantCode",
                table: "Plants",
                column: "PlantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RobotModels_Type",
                table: "RobotModels",
                column: "Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Robots_CurrentAreaId",
                table: "Robots",
                column: "CurrentAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Robots_ModelId",
                table: "Robots",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_SafePositions_AreaId",
                table: "SafePositions",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoStream_RobotId",
                table: "VideoStream",
                column: "RobotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inspection");

            migrationBuilder.DropTable(
                name: "MissionDefinitions");

            migrationBuilder.DropTable(
                name: "SafePositions");

            migrationBuilder.DropTable(
                name: "VideoStream");

            migrationBuilder.DropTable(
                name: "MissionTask");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropTable(
                name: "MissionRuns");

            migrationBuilder.DropTable(
                name: "Robots");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "RobotModels");

            migrationBuilder.DropTable(
                name: "Decks");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "Installations");
        }
    }
}
