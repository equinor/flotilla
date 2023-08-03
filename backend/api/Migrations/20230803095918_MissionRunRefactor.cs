using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class MissionRunRefactor : Migration
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
                name: "FK_SafePositions_AssetDecks_AssetDeckId",
                table: "SafePositions");

            migrationBuilder.DropTable(
                name: "AssetDecks");

            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.RenameColumn(
                name: "AssetDeckId",
                table: "SafePositions",
                newName: "AreaId");

            migrationBuilder.RenameIndex(
                name: "IX_SafePositions_AssetDeckId",
                table: "SafePositions",
                newName: "IX_SafePositions_AreaId");

            migrationBuilder.RenameColumn(
                name: "CurrentAssetDeckId",
                table: "Robots",
                newName: "CurrentAreaId");

            migrationBuilder.RenameColumn(
                name: "CurrentAsset",
                table: "Robots",
                newName: "CurrentInstallation");

            migrationBuilder.RenameIndex(
                name: "IX_Robots_CurrentAssetDeckId",
                table: "Robots",
                newName: "IX_Robots_CurrentAreaId");

            migrationBuilder.RenameColumn(
                name: "MissionId",
                table: "MissionTask",
                newName: "MissionRunId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionTask_MissionId",
                table: "MissionTask",
                newName: "IX_MissionTask_MissionRunId");

            migrationBuilder.CreateTable(
                name: "Installations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstallationCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstallationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PlantCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
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
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    InstallationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
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
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeckId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstallationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MapMetadata_MapName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MapMetadata_Boundary_X1 = table.Column<double>(type: "float", nullable: false),
                    MapMetadata_Boundary_X2 = table.Column<double>(type: "float", nullable: false),
                    MapMetadata_Boundary_Y1 = table.Column<double>(type: "float", nullable: false),
                    MapMetadata_Boundary_Y2 = table.Column<double>(type: "float", nullable: false),
                    MapMetadata_Boundary_Z1 = table.Column<double>(type: "float", nullable: false),
                    MapMetadata_Boundary_Z2 = table.Column<double>(type: "float", nullable: false),
                    MapMetadata_TransformationMatrices_C1 = table.Column<double>(type: "float", nullable: false),
                    MapMetadata_TransformationMatrices_C2 = table.Column<double>(type: "float", nullable: false),
                    MapMetadata_TransformationMatrices_D1 = table.Column<double>(type: "float", nullable: false),
                    MapMetadata_TransformationMatrices_D2 = table.Column<double>(type: "float", nullable: false),
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
                name: "MissionRuns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MissionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InstallationCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DesiredStartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RobotId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsarMissionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StatusReason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AreaId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Map_MapName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Map_Boundary_X1 = table.Column<double>(type: "float", nullable: true),
                    Map_Boundary_X2 = table.Column<double>(type: "float", nullable: true),
                    Map_Boundary_Y1 = table.Column<double>(type: "float", nullable: true),
                    Map_Boundary_Y2 = table.Column<double>(type: "float", nullable: true),
                    Map_Boundary_Z1 = table.Column<double>(type: "float", nullable: true),
                    Map_Boundary_Z2 = table.Column<double>(type: "float", nullable: true),
                    Map_TransformationMatrices_C1 = table.Column<double>(type: "float", nullable: true),
                    Map_TransformationMatrices_C2 = table.Column<double>(type: "float", nullable: true),
                    Map_TransformationMatrices_D1 = table.Column<double>(type: "float", nullable: true),
                    Map_TransformationMatrices_D2 = table.Column<double>(type: "float", nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                name: "MissionDefinitions",
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
                name: "IX_Plants_InstallationId",
                table: "Plants",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_PlantCode",
                table: "Plants",
                column: "PlantCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTask_MissionRuns_MissionRunId",
                table: "MissionTask",
                column: "MissionRunId",
                principalTable: "MissionRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Areas_CurrentAreaId",
                table: "Robots",
                column: "CurrentAreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SafePositions_Areas_AreaId",
                table: "SafePositions",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionTask_MissionRuns_MissionRunId",
                table: "MissionTask");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Areas_CurrentAreaId",
                table: "Robots");

            migrationBuilder.DropForeignKey(
                name: "FK_SafePositions_Areas_AreaId",
                table: "SafePositions");

            migrationBuilder.DropTable(
                name: "MissionDefinitions");

            migrationBuilder.DropTable(
                name: "MissionRuns");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "Decks");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "Installations");

            migrationBuilder.RenameColumn(
                name: "AreaId",
                table: "SafePositions",
                newName: "AssetDeckId");

            migrationBuilder.RenameIndex(
                name: "IX_SafePositions_AreaId",
                table: "SafePositions",
                newName: "IX_SafePositions_AssetDeckId");

            migrationBuilder.RenameColumn(
                name: "CurrentInstallation",
                table: "Robots",
                newName: "CurrentAsset");

            migrationBuilder.RenameColumn(
                name: "CurrentAreaId",
                table: "Robots",
                newName: "CurrentAssetDeckId");

            migrationBuilder.RenameIndex(
                name: "IX_Robots_CurrentAreaId",
                table: "Robots",
                newName: "IX_Robots_CurrentAssetDeckId");

            migrationBuilder.RenameColumn(
                name: "MissionRunId",
                table: "MissionTask",
                newName: "MissionId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionTask_MissionRunId",
                table: "MissionTask",
                newName: "IX_MissionTask_MissionId");

            migrationBuilder.CreateTable(
                name: "AssetDecks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeckName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DefaultLocalizationPose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Position_X = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    DefaultLocalizationPose_Position_Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RobotId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DesiredStartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EchoMissionId = table.Column<int>(type: "int", maxLength: 200, nullable: true),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EstimatedDuration = table.Column<long>(type: "bigint", nullable: true),
                    IsarMissionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    MapMetadata_MapName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MapMetadata_Boundary_X1 = table.Column<double>(type: "float", nullable: true),
                    MapMetadata_Boundary_X2 = table.Column<double>(type: "float", nullable: true),
                    MapMetadata_Boundary_Y1 = table.Column<double>(type: "float", nullable: true),
                    MapMetadata_Boundary_Y2 = table.Column<double>(type: "float", nullable: true),
                    MapMetadata_Boundary_Z1 = table.Column<double>(type: "float", nullable: true),
                    MapMetadata_Boundary_Z2 = table.Column<double>(type: "float", nullable: true),
                    MapMetadata_TransformationMatrices_C1 = table.Column<double>(type: "float", nullable: true),
                    MapMetadata_TransformationMatrices_C2 = table.Column<double>(type: "float", nullable: true),
                    MapMetadata_TransformationMatrices_D1 = table.Column<double>(type: "float", nullable: true),
                    MapMetadata_TransformationMatrices_D2 = table.Column<double>(type: "float", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_AssetDecks_AssetCode_DeckName",
                table: "AssetDecks",
                columns: new[] { "AssetCode", "DeckName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Missions_RobotId",
                table: "Missions",
                column: "RobotId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTask_Missions_MissionId",
                table: "MissionTask",
                column: "MissionId",
                principalTable: "Missions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_AssetDecks_CurrentAssetDeckId",
                table: "Robots",
                column: "CurrentAssetDeckId",
                principalTable: "AssetDecks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SafePositions_AssetDecks_AssetDeckId",
                table: "SafePositions",
                column: "AssetDeckId",
                principalTable: "AssetDecks",
                principalColumn: "Id");
        }
    }
}
