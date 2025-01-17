using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class ResetDatabaseStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DefaultLocalizationPoses",
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
                    DockingEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultLocalizationPoses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inspections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IsarTaskId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsarInspectionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InspectionTarget_X = table.Column<float>(type: "real", nullable: false),
                    InspectionTarget_Y = table.Column<float>(type: "real", nullable: false),
                    InspectionTarget_Z = table.Column<float>(type: "real", nullable: false),
                    InspectionTargetName = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    InspectionType = table.Column<string>(type: "text", nullable: false),
                    VideoDuration = table.Column<float>(type: "real", nullable: true),
                    AnalysisType = table.Column<string>(type: "text", nullable: true),
                    InspectionUrl = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspections", x => x.Id);
                });

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
                    BatteryMissionStartThreshold = table.Column<float>(type: "real", nullable: true),
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
                    CustomMissionTasks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagInspectionMetadata",
                columns: table => new
                {
                    TagId = table.Column<string>(type: "text", nullable: false),
                    ZoomDescription_ObjectWidth = table.Column<double>(type: "double precision", nullable: true),
                    ZoomDescription_ObjectHeight = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagInspectionMetadata", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "UserInfos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Oid = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InspectionFindings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsarTaskId = table.Column<string>(type: "text", nullable: false),
                    Finding = table.Column<string>(type: "text", nullable: false),
                    InspectionId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionFindings_Inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccessRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    InstallationId = table.Column<string>(type: "text", nullable: true),
                    RoleName = table.Column<string>(type: "text", nullable: false),
                    AccessLevel = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessRoles_Installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id");
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
                name: "InspectionAreas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PlantId = table.Column<string>(type: "text", nullable: false),
                    InstallationId = table.Column<string>(type: "text", nullable: false),
                    DefaultLocalizationPoseId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionAreas_DefaultLocalizationPoses_DefaultLocalizatio~",
                        column: x => x.DefaultLocalizationPoseId,
                        principalTable: "DefaultLocalizationPoses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InspectionAreas_Installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InspectionAreas_Plants_PlantId",
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
                    InspectionAreaId = table.Column<string>(type: "text", nullable: false),
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
                    DefaultLocalizationPoseId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Areas_DefaultLocalizationPoses_DefaultLocalizationPoseId",
                        column: x => x.DefaultLocalizationPoseId,
                        principalTable: "DefaultLocalizationPoses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Areas_InspectionAreas_InspectionAreaId",
                        column: x => x.InspectionAreaId,
                        principalTable: "InspectionAreas",
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
                    CurrentInstallationId = table.Column<string>(type: "text", nullable: false),
                    CurrentInspectionAreaId = table.Column<string>(type: "text", nullable: true),
                    BatteryLevel = table.Column<float>(type: "real", nullable: false),
                    BatteryState = table.Column<string>(type: "text", nullable: true),
                    PressureLevel = table.Column<float>(type: "real", nullable: true),
                    Host = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    RobotCapabilities = table.Column<string>(type: "text", nullable: true),
                    IsarConnected = table.Column<bool>(type: "boolean", nullable: false),
                    Deprecated = table.Column<bool>(type: "boolean", nullable: false),
                    MissionQueueFrozen = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FlotillaStatus = table.Column<string>(type: "text", nullable: false),
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
                        name: "FK_Robots_InspectionAreas_CurrentInspectionAreaId",
                        column: x => x.CurrentInspectionAreaId,
                        principalTable: "InspectionAreas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Robots_Installations_CurrentInstallationId",
                        column: x => x.CurrentInstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Robots_RobotModels_ModelId",
                        column: x => x.ModelId,
                        principalTable: "RobotModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentInfo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentInfo_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionRuns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MissionId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    InstallationCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DesiredStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    MissionRunType = table.Column<string>(type: "text", nullable: false),
                    IsarMissionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    StatusReason = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InspectionAreaId = table.Column<string>(type: "text", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedDuration = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeprecated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionRuns_InspectionAreas_InspectionAreaId",
                        column: x => x.InspectionAreaId,
                        principalTable: "InspectionAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionRuns_Robots_RobotId",
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
                    LastSuccessfulRunId = table.Column<string>(type: "text", nullable: true),
                    InspectionAreaId = table.Column<string>(type: "text", nullable: true),
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
                    IsDeprecated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionDefinitions_InspectionAreas_InspectionAreaId",
                        column: x => x.InspectionAreaId,
                        principalTable: "InspectionAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionDefinitions_MissionRuns_LastSuccessfulRunId",
                        column: x => x.LastSuccessfulRunId,
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
                name: "MissionTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IsarTaskId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TaskOrder = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    TagId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TagLink = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RobotPose_Position_X = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Position_Z = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    RobotPose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    PoseId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsarZoomDescription_ObjectWidth = table.Column<double>(type: "double precision", nullable: true),
                    IsarZoomDescription_ObjectHeight = table.Column<double>(type: "double precision", nullable: true),
                    InspectionId = table.Column<string>(type: "text", nullable: true),
                    MissionRunId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionTasks_Inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MissionTasks_MissionRuns_MissionRunId",
                        column: x => x.MissionRunId,
                        principalTable: "MissionRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRoles_InstallationId",
                table: "AccessRoles",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_DefaultLocalizationPoseId",
                table: "Areas",
                column: "DefaultLocalizationPoseId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_InspectionAreaId",
                table: "Areas",
                column: "InspectionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_InstallationId",
                table: "Areas",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_PlantId",
                table: "Areas",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentInfo_RobotId",
                table: "DocumentInfo",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAreas_DefaultLocalizationPoseId",
                table: "InspectionAreas",
                column: "DefaultLocalizationPoseId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAreas_InstallationId",
                table: "InspectionAreas",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAreas_PlantId",
                table: "InspectionAreas",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionFindings_InspectionId",
                table: "InspectionFindings",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Installations_InstallationCode",
                table: "Installations",
                column: "InstallationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinitions_InspectionAreaId",
                table: "MissionDefinitions",
                column: "InspectionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinitions_LastSuccessfulRunId",
                table: "MissionDefinitions",
                column: "LastSuccessfulRunId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinitions_SourceId",
                table: "MissionDefinitions",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionRuns_InspectionAreaId",
                table: "MissionRuns",
                column: "InspectionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionRuns_RobotId",
                table: "MissionRuns",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTasks_InspectionId",
                table: "MissionTasks",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTasks_MissionRunId",
                table: "MissionTasks",
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
                name: "IX_Robots_CurrentInspectionAreaId",
                table: "Robots",
                column: "CurrentInspectionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Robots_CurrentInstallationId",
                table: "Robots",
                column: "CurrentInstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Robots_ModelId",
                table: "Robots",
                column: "ModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessRoles");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "DocumentInfo");

            migrationBuilder.DropTable(
                name: "InspectionFindings");

            migrationBuilder.DropTable(
                name: "MissionDefinitions");

            migrationBuilder.DropTable(
                name: "MissionTasks");

            migrationBuilder.DropTable(
                name: "TagInspectionMetadata");

            migrationBuilder.DropTable(
                name: "UserInfos");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropTable(
                name: "Inspections");

            migrationBuilder.DropTable(
                name: "MissionRuns");

            migrationBuilder.DropTable(
                name: "Robots");

            migrationBuilder.DropTable(
                name: "InspectionAreas");

            migrationBuilder.DropTable(
                name: "RobotModels");

            migrationBuilder.DropTable(
                name: "DefaultLocalizationPoses");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "Installations");
        }
    }
}
