using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAcousticInspectionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcousticInspectionMetadata_DetectionType",
                table: "TaskDefinition",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AcousticInspectionMetadata_FrequencyFrom",
                table: "TaskDefinition",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AcousticInspectionMetadata_FrequencyTo",
                table: "TaskDefinition",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AcousticInspectionMetadata_SnrValueThreshold",
                table: "TaskDefinition",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_Height",
                table: "TaskDefinition",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_Width",
                table: "TaskDefinition",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_X",
                table: "TaskDefinition",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_Y",
                table: "TaskDefinition",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcousticInspectionMetadata_DetectionType",
                table: "Inspections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AcousticInspectionMetadata_FrequencyFrom",
                table: "Inspections",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AcousticInspectionMetadata_FrequencyTo",
                table: "Inspections",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AcousticInspectionMetadata_SnrValueThreshold",
                table: "Inspections",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_Height",
                table: "Inspections",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_Width",
                table: "Inspections",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_X",
                table: "Inspections",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcousticInspectionMetadata_Roi_Y",
                table: "Inspections",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_DetectionType",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_FrequencyFrom",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_FrequencyTo",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_SnrValueThreshold",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_Height",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_Width",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_X",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_Y",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_DetectionType",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_FrequencyFrom",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_FrequencyTo",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_SnrValueThreshold",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_Height",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_Width",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_X",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "AcousticInspectionMetadata_Roi_Y",
                table: "Inspections");
        }
    }
}
