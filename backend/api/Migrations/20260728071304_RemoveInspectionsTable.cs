using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInspectionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionTasks_Inspections_InspectionId",
                table: "MissionTasks");

            migrationBuilder.DropIndex(
                name: "IX_MissionTasks_InspectionId",
                table: "MissionTasks");

            migrationBuilder.AddColumn<string>(
                name: "AcousticInspectionMetadata_DetectionType",
                table: "MissionTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AcousticInspectionMetadata_FrequencyFrom",
                table: "MissionTasks",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AcousticInspectionMetadata_FrequencyTo",
                table: "MissionTasks",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_Height",
                table: "MissionTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_Width",
                table: "MissionTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_X",
                table: "MissionTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_Y",
                table: "MissionTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AcousticInspectionMetadata_SnrValueThreshold",
                table: "MissionTasks",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SensorType",
                table: "MissionTasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "TargetPosition_X",
                table: "MissionTasks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "TargetPosition_Y",
                table: "MissionTasks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "TargetPosition_Z",
                table: "MissionTasks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "VideoDuration",
                table: "MissionTasks",
                type: "real",
                nullable: true);

            // Copy data from Inspections into MissionTasks before dropping the table
            migrationBuilder.Sql(
                """
                UPDATE "MissionTasks" mt
                SET
                    "SensorType"                                  = i."InspectionType",
                    "VideoDuration"                               = i."VideoDuration",
                    "TargetPosition_X"                            = i."InspectionTarget_X",
                    "TargetPosition_Y"                            = i."InspectionTarget_Y",
                    "TargetPosition_Z"                            = i."InspectionTarget_Z",
                    "AcousticInspectionMetadata_DetectionType"    = i."AcousticInspectionMetadata_DetectionType",
                    "AcousticInspectionMetadata_FrequencyFrom"    = i."AcousticInspectionMetadata_FrequencyFrom",
                    "AcousticInspectionMetadata_FrequencyTo"      = i."AcousticInspectionMetadata_FrequencyTo",
                    "AcousticInspectionMetadata_SnrValueThreshold"= i."AcousticInspectionMetadata_SnrValueThreshold",
                    "AcousticInspectionMetadata_Roi_Height"       = i."AcousticInspectionMetadata_Roi_Height",
                    "AcousticInspectionMetadata_Roi_Width"        = i."AcousticInspectionMetadata_Roi_Width",
                    "AcousticInspectionMetadata_Roi_X"            = i."AcousticInspectionMetadata_Roi_X",
                    "AcousticInspectionMetadata_Roi_Y"            = i."AcousticInspectionMetadata_Roi_Y"
                FROM "Inspections" i
                WHERE mt."InspectionId" = i."Id";
                """
            );

            migrationBuilder.DropTable(
                name: "Inspections");

            migrationBuilder.DropColumn(
                name: "InspectionId",
                table: "MissionTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the Inspections table
            migrationBuilder.CreateTable(
                name: "Inspections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AnalysisTypes = table.Column<string[]>(type: "text[]", nullable: true),
                    InspectionType = table.Column<string>(type: "text", nullable: false),
                    IsarInspectionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VideoDuration = table.Column<float>(type: "real", nullable: true),
                    AcousticInspectionMetadata_DetectionType = table.Column<string>(type: "text", nullable: true),
                    AcousticInspectionMetadata_FrequencyFrom = table.Column<float>(type: "real", nullable: true),
                    AcousticInspectionMetadata_FrequencyTo = table.Column<float>(type: "real", nullable: true),
                    AcousticInspectionMetadata_SnrValueThreshold = table.Column<float>(type: "real", nullable: true),
                    AcousticInspectionMetadata_Roi_Height = table.Column<int>(type: "integer", nullable: true),
                    AcousticInspectionMetadata_Roi_Width = table.Column<int>(type: "integer", nullable: true),
                    AcousticInspectionMetadata_Roi_X = table.Column<int>(type: "integer", nullable: true),
                    AcousticInspectionMetadata_Roi_Y = table.Column<int>(type: "integer", nullable: true),
                    InspectionTarget_X = table.Column<float>(type: "real", nullable: false),
                    InspectionTarget_Y = table.Column<float>(type: "real", nullable: false),
                    InspectionTarget_Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspections", x => x.Id);
                });

            // Add InspectionId back to MissionTasks and populate with new GUIDs,
            // copying data back from MissionTasks into Inspections (best effort — original IDs are lost)
            migrationBuilder.AddColumn<string>(
                name: "InspectionId",
                table: "MissionTasks",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Inspections" (
                    "Id",
                    "InspectionType",
                    "IsarInspectionId",
                    "VideoDuration",
                    "InspectionTarget_X",
                    "InspectionTarget_Y",
                    "InspectionTarget_Z",
                    "AcousticInspectionMetadata_DetectionType",
                    "AcousticInspectionMetadata_FrequencyFrom",
                    "AcousticInspectionMetadata_FrequencyTo",
                    "AcousticInspectionMetadata_SnrValueThreshold",
                    "AcousticInspectionMetadata_Roi_Height",
                    "AcousticInspectionMetadata_Roi_Width",
                    "AcousticInspectionMetadata_Roi_X",
                    "AcousticInspectionMetadata_Roi_Y"
                )
                SELECT
                    gen_random_uuid()::text,
                    mt."SensorType",
                    gen_random_uuid()::text,
                    mt."VideoDuration",
                    mt."TargetPosition_X",
                    mt."TargetPosition_Y",
                    mt."TargetPosition_Z",
                    mt."AcousticInspectionMetadata_DetectionType",
                    mt."AcousticInspectionMetadata_FrequencyFrom",
                    mt."AcousticInspectionMetadata_FrequencyTo",
                    mt."AcousticInspectionMetadata_SnrValueThreshold",
                    mt."AcousticInspectionMetadata_Roi_Height",
                    mt."AcousticInspectionMetadata_Roi_Width",
                    mt."AcousticInspectionMetadata_Roi_X",
                    mt."AcousticInspectionMetadata_Roi_Y"
                FROM "MissionTasks" mt;
                """
            );

            // Link each MissionTask to its newly created Inspection row.
            // Since there is a 1-to-1 relationship we can match on all copied fields.
            migrationBuilder.Sql(
                """
                UPDATE "MissionTasks" mt
                SET "InspectionId" = i."Id"
                FROM "Inspections" i
                WHERE i."InspectionType"    = mt."SensorType"
                  AND i."InspectionTarget_X" = mt."TargetPosition_X"
                  AND i."InspectionTarget_Y" = mt."TargetPosition_Y"
                  AND i."InspectionTarget_Z" = mt."TargetPosition_Z";
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_MissionTasks_InspectionId",
                table: "MissionTasks",
                column: "InspectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTasks_Inspections_InspectionId",
                table: "MissionTasks",
                column: "InspectionId",
                principalTable: "Inspections",
                principalColumn: "Id");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_DetectionType",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_FrequencyFrom",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_FrequencyTo",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_Height",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_Width",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_X",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_Y",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_SnrValueThreshold",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "SensorType",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "TargetPosition_X",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "TargetPosition_Y",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "TargetPosition_Z",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "VideoDuration",
                table: "MissionTasks");
        }
    }
}
