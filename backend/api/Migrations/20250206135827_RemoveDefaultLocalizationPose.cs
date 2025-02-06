using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultLocalizationPose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_DefaultLocalizationPoses_DefaultLocalizationPoseId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionAreas_DefaultLocalizationPoses_DefaultLocalizatio~",
                table: "InspectionAreas");

            migrationBuilder.DropTable(
                name: "DefaultLocalizationPoses");

            migrationBuilder.DropIndex(
                name: "IX_InspectionAreas_DefaultLocalizationPoseId",
                table: "InspectionAreas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_DefaultLocalizationPoseId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPoseId",
                table: "InspectionAreas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPoseId",
                table: "Areas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultLocalizationPoseId",
                table: "InspectionAreas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultLocalizationPoseId",
                table: "Areas",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DefaultLocalizationPoses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Pose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultLocalizationPoses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAreas_DefaultLocalizationPoseId",
                table: "InspectionAreas",
                column: "DefaultLocalizationPoseId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_DefaultLocalizationPoseId",
                table: "Areas",
                column: "DefaultLocalizationPoseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_DefaultLocalizationPoses_DefaultLocalizationPoseId",
                table: "Areas",
                column: "DefaultLocalizationPoseId",
                principalTable: "DefaultLocalizationPoses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionAreas_DefaultLocalizationPoses_DefaultLocalizatio~",
                table: "InspectionAreas",
                column: "DefaultLocalizationPoseId",
                principalTable: "DefaultLocalizationPoses",
                principalColumn: "Id");
        }
    }
}
