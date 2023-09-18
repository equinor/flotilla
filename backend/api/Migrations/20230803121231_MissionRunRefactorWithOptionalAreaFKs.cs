using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class MissionRunRefactorWithOptionalAreaFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Decks_Installations_InstallationId",
                table: "Decks");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_Plants_PlantId",
                table: "Decks");

            migrationBuilder.DropForeignKey(
                name: "FK_Plants_Installations_InstallationId",
                table: "Plants");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Areas_CurrentAssetDeckId",
                table: "Robots");

            migrationBuilder.DropIndex(
                name: "IX_Areas_AssetCode_DeckName",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "EchoMissionId",
                table: "MissionRuns");

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
                name: "MapMetadata_TransformationMatrices_D2",
                table: "MissionRuns",
                newName: "Map_TransformationMatrices_D2");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_TransformationMatrices_D1",
                table: "MissionRuns",
                newName: "Map_TransformationMatrices_D1");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_TransformationMatrices_C2",
                table: "MissionRuns",
                newName: "Map_TransformationMatrices_C2");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_TransformationMatrices_C1",
                table: "MissionRuns",
                newName: "Map_TransformationMatrices_C1");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_MapName",
                table: "MissionRuns",
                newName: "Map_MapName");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_Z2",
                table: "MissionRuns",
                newName: "Map_Boundary_Z2");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_Z1",
                table: "MissionRuns",
                newName: "Map_Boundary_Z1");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_Y2",
                table: "MissionRuns",
                newName: "Map_Boundary_Y2");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_Y1",
                table: "MissionRuns",
                newName: "Map_Boundary_Y1");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_X2",
                table: "MissionRuns",
                newName: "Map_Boundary_X2");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_Boundary_X1",
                table: "MissionRuns",
                newName: "Map_Boundary_X1");

            migrationBuilder.RenameColumn(
                name: "AssetCode",
                table: "MissionRuns",
                newName: "InstallationCode");

            migrationBuilder.RenameColumn(
                name: "DeckName",
                table: "Areas",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "AssetCode",
                table: "Areas",
                newName: "MapMetadata_MapName");

            migrationBuilder.AddColumn<string>(
                name: "AreaId",
                table: "MissionRuns",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissionId",
                table: "MissionRuns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeckId",
                table: "Areas",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstallationId",
                table: "Areas",
                type: "nvarchar(450)",
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "PlantId",
                table: "Areas",
                type: "nvarchar(450)",
                nullable: true);

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
                name: "IX_Plants_PlantCode",
                table: "Plants",
                column: "PlantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionRuns_AreaId",
                table: "MissionRuns",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Installations_InstallationCode",
                table: "Installations",
                column: "InstallationCode",
                unique: true);

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
                name: "FK_MissionRuns_Areas_AreaId",
                table: "MissionRuns",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

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
                name: "FK_MissionRuns_Areas_AreaId",
                table: "MissionRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Plants_Installations_InstallationId",
                table: "Plants");

            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Areas_CurrentAreaId",
                table: "Robots");

            migrationBuilder.DropTable(
                name: "MissionDefinitions");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropIndex(
                name: "IX_Plants_PlantCode",
                table: "Plants");

            migrationBuilder.DropIndex(
                name: "IX_MissionRuns_AreaId",
                table: "MissionRuns");

            migrationBuilder.DropIndex(
                name: "IX_Installations_InstallationCode",
                table: "Installations");

            migrationBuilder.DropIndex(
                name: "IX_Areas_DeckId",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_InstallationId",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_PlantId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "MissionId",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "DeckId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "InstallationId",
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

            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "Areas");

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
                name: "Map_TransformationMatrices_D2",
                table: "MissionRuns",
                newName: "MapMetadata_TransformationMatrices_D2");

            migrationBuilder.RenameColumn(
                name: "Map_TransformationMatrices_D1",
                table: "MissionRuns",
                newName: "MapMetadata_TransformationMatrices_D1");

            migrationBuilder.RenameColumn(
                name: "Map_TransformationMatrices_C2",
                table: "MissionRuns",
                newName: "MapMetadata_TransformationMatrices_C2");

            migrationBuilder.RenameColumn(
                name: "Map_TransformationMatrices_C1",
                table: "MissionRuns",
                newName: "MapMetadata_TransformationMatrices_C1");

            migrationBuilder.RenameColumn(
                name: "Map_MapName",
                table: "MissionRuns",
                newName: "MapMetadata_MapName");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_Z2",
                table: "MissionRuns",
                newName: "MapMetadata_Boundary_Z2");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_Z1",
                table: "MissionRuns",
                newName: "MapMetadata_Boundary_Z1");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_Y2",
                table: "MissionRuns",
                newName: "MapMetadata_Boundary_Y2");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_Y1",
                table: "MissionRuns",
                newName: "MapMetadata_Boundary_Y1");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_X2",
                table: "MissionRuns",
                newName: "MapMetadata_Boundary_X2");

            migrationBuilder.RenameColumn(
                name: "Map_Boundary_X1",
                table: "MissionRuns",
                newName: "MapMetadata_Boundary_X1");

            migrationBuilder.RenameColumn(
                name: "InstallationCode",
                table: "MissionRuns",
                newName: "AssetCode");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Areas",
                newName: "DeckName");

            migrationBuilder.RenameColumn(
                name: "MapMetadata_MapName",
                table: "Areas",
                newName: "AssetCode");

            migrationBuilder.AddColumn<int>(
                name: "EchoMissionId",
                table: "MissionRuns",
                type: "int",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Areas_AssetCode_DeckName",
                table: "Areas",
                columns: new[] { "AssetCode", "DeckName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_Installations_InstallationId",
                table: "Decks",
                column: "InstallationId",
                principalTable: "Installations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_Plants_PlantId",
                table: "Decks",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Plants_Installations_InstallationId",
                table: "Plants",
                column: "InstallationId",
                principalTable: "Installations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Areas_CurrentAssetDeckId",
                table: "Robots",
                column: "CurrentAssetDeckId",
                principalTable: "Areas",
                principalColumn: "Id");
        }
    }
}
