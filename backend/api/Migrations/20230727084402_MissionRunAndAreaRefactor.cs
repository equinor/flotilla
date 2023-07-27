using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class MissionRunAndAreaRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionTask_Missions_MissionId",
                table: "MissionTask");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_AssetDecks_CurrentAssetDeckId",
                table: "Robots");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_RobotModels_ModelId",
                table: "Robots");

            migrationBuilder.DropForeignKey(
                name: "FK_SafePositions_AssetDecks_AssetDeckId",
                table: "SafePositions");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoStream_Robots_RobotId",
                table: "VideoStream");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SafePositions",
                table: "SafePositions");

            migrationBuilder.DropForeignKey(
                name: "FK_Missions_Robots_RobotId",
                table: "Missions"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_Robots",
                table: "Robots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RobotModels",
                table: "RobotModels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AssetDecks",
                table: "AssetDecks");

            migrationBuilder.DropIndex(
                name: "IX_AssetDecks_AssetCode_DeckName",
                table: "AssetDecks");

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
                name: "DefaultLocalizationPose_Orientation_W",
                table: "AssetDecks");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Orientation_X",
                table: "AssetDecks");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Orientation_Y",
                table: "AssetDecks");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Orientation_Z",
                table: "AssetDecks");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Position_X",
                table: "AssetDecks");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Position_Y",
                table: "AssetDecks");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Position_Z",
                table: "AssetDecks");

            migrationBuilder.DropColumn(
                name: "EchoMissionId",
                table: "Missions"
            );

            migrationBuilder.RenameColumn(
                name: "AssetCode",
                table: "Missions",
                newName: "InstallationCode"
            );

            migrationBuilder.AddColumn<string>(
                name: "AreaId",
                table: "Missions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissionId",
                table: "Missions",
                type: "nvarchar(max)",
                nullable: true);

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
                name: "AssetDecks",
                newName: "AssetDeck");

            migrationBuilder.RenameTable(
                name: "Missions",
                newName: "MissionRun");

            migrationBuilder.RenameColumn(
                name: "MissionId",
                table: "MissionTask",
                newName: "MissionRunId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionTask_MissionId",
                table: "MissionTask",
                newName: "IX_MissionTask_MissionRunId");

            migrationBuilder.RenameColumn(
                name: "AssetDeckId",
                table: "SafePosition",
                newName: "AreaId");

            migrationBuilder.RenameIndex(
                name: "IX_SafePositions_AssetDeckId",
                table: "SafePosition",
                newName: "IX_SafePosition_AreaId");

            migrationBuilder.RenameColumn(
                name: "CurrentAssetDeckId",
                table: "Robot",
                newName: "CurrentAreaId");

            migrationBuilder.RenameColumn(
                name: "CurrentAsset",
                table: "Robot",
                newName: "CurrentInstallation");

            migrationBuilder.RenameIndex(
                name: "IX_Robots_ModelId",
                table: "Robot",
                newName: "IX_Robot_ModelId");

            migrationBuilder.RenameIndex(
                name: "IX_Robots_CurrentAssetDeckId",
                table: "Robot",
                newName: "IX_Robot_CurrentAreaId");

            migrationBuilder.RenameIndex(
                name: "IX_RobotModels_Type",
                table: "RobotModel",
                newName: "IX_RobotModel_Type");

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
                name: "PK_AssetDeck",
                table: "AssetDeck",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Installation",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstallationCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installation", x => x.Id);
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
                name: "Source",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Source", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstallationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PlantCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plant_Installation_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MissionTask.RobotPose#Pose.Orientation#Orientation",
                columns: table => new
                {
                    PoseMissionTaskId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false),
                    W = table.Column<float>(type: "real", nullable: false)
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
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false),
                    W = table.Column<float>(type: "real", nullable: false)
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
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false),
                    W = table.Column<float>(type: "real", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Deck",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    InstallationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deck", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deck_Installation_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deck_Plant_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Area",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeckId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstallationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Area", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Area_Deck_DeckId",
                        column: x => x.DeckId,
                        principalTable: "Deck",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Area_Installation_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Area_Plant_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "Area.DefaultLocalizationPose#Pose.Orientation#Orientation",
                columns: table => new
                {
                    PoseAreaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false),
                    W = table.Column<float>(type: "real", nullable: false)
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
                name: "MissionDefinition",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstallationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InspectionFrequency = table.Column<TimeSpan>(type: "time", nullable: true),
                    LastRunId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AreaId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeprecated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionDefinition_Area_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Area",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MissionDefinition_MissionRun_LastRunId",
                        column: x => x.LastRunId,
                        principalTable: "MissionRun",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MissionDefinition_Source_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Source",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateIndex(
                name: "IX_Area_DeckId",
                table: "Area",
                column: "DeckId");

            migrationBuilder.CreateIndex(
                name: "IX_Area_InstallationId",
                table: "Area",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Area_PlantId",
                table: "Area",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Deck_InstallationId",
                table: "Deck",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Deck_PlantId",
                table: "Deck",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Installation_InstallationCode",
                table: "Installation",
                column: "InstallationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinition_AreaId",
                table: "MissionDefinition",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinition_LastRunId",
                table: "MissionDefinition",
                column: "LastRunId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinition_SourceId",
                table: "MissionDefinition",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionRun_AreaId",
                table: "MissionRun",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionRun_RobotId",
                table: "MissionRun",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_Plant_InstallationId",
                table: "Plant",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Plant_PlantCode",
                table: "Plant",
                column: "PlantCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTask_MissionRun_MissionRunId",
                table: "MissionTask",
                column: "MissionRunId",
                principalTable: "MissionRun",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionTask_MissionRun_MissionRunId",
                table: "MissionTask");

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
                name: "MissionDefinition");

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
                name: "Source");

            migrationBuilder.DropTable(
                name: "MissionRun.MapMetadata#MapMetadata");

            migrationBuilder.DropTable(
                name: "MissionTask.RobotPose#Pose");

            migrationBuilder.DropTable(
                name: "Robot.Pose#Pose");

            migrationBuilder.DropTable(
                name: "SafePosition.Pose#Pose");

            migrationBuilder.DropTable(
                name: "MissionRun");

            migrationBuilder.DropTable(
                name: "Area");

            migrationBuilder.DropTable(
                name: "Deck");

            migrationBuilder.DropTable(
                name: "Plant");

            migrationBuilder.DropTable(
                name: "Installation");

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
                name: "PK_AssetDeck",
                table: "AssetDeck");

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
                name: "AssetDeck",
                newName: "AssetDecks");

            migrationBuilder.RenameColumn(
                name: "MissionRunId",
                table: "MissionTask",
                newName: "MissionId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionTask_MissionRunId",
                table: "MissionTask",
                newName: "IX_MissionTask_MissionId");

            migrationBuilder.RenameColumn(
                name: "AreaId",
                table: "SafePositions",
                newName: "AssetDeckId");

            migrationBuilder.RenameIndex(
                name: "IX_SafePosition_AreaId",
                table: "SafePositions",
                newName: "IX_SafePositions_AssetDeckId");

            migrationBuilder.RenameIndex(
                name: "IX_RobotModel_Type",
                table: "RobotModels",
                newName: "IX_RobotModels_Type");

            migrationBuilder.RenameColumn(
                name: "CurrentInstallation",
                table: "Robots",
                newName: "CurrentAsset");

            migrationBuilder.RenameColumn(
                name: "CurrentAreaId",
                table: "Robots",
                newName: "CurrentAssetDeckId");

            migrationBuilder.RenameIndex(
                name: "IX_Robot_ModelId",
                table: "Robots",
                newName: "IX_Robots_ModelId");

            migrationBuilder.RenameIndex(
                name: "IX_Robot_CurrentAreaId",
                table: "Robots",
                newName: "IX_Robots_CurrentAssetDeckId");

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

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_W",
                table: "AssetDecks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_X",
                table: "AssetDecks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_Y",
                table: "AssetDecks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_Z",
                table: "AssetDecks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Position_X",
                table: "AssetDecks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Position_Y",
                table: "AssetDecks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Position_Z",
                table: "AssetDecks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

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
                name: "PK_AssetDecks",
                table: "AssetDecks",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDecks_AssetCode_DeckName",
                table: "AssetDecks",
                columns: new[] { "AssetCode", "DeckName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionRun_RobotId",
                table: "MissionRun",
                column: "RobotId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTask_MissionRun_MissionId",
                table: "MissionTask",
                column: "MissionId",
                principalTable: "MissionRun",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_AssetDecks_CurrentAssetDeckId",
                table: "Robots",
                column: "CurrentAssetDeckId",
                principalTable: "AssetDecks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_RobotModels_ModelId",
                table: "Robots",
                column: "ModelId",
                principalTable: "RobotModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SafePositions_AssetDecks_AssetDeckId",
                table: "SafePositions",
                column: "AssetDeckId",
                principalTable: "AssetDecks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoStream_Robots_RobotId",
                table: "VideoStream",
                column: "RobotId",
                principalTable: "Robots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
