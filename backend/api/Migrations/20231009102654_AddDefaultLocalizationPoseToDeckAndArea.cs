using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultLocalizationPoseToDeckAndArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Orientation_W",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Orientation_X",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Orientation_Y",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Orientation_Z",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Position_X",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Position_Y",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPose_Position_Z",
                table: "Areas");

            migrationBuilder.AddColumn<string>(
                name: "DefaultLocalizationPoseId",
                table: "Decks",
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
                    Pose_Position_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_W = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultLocalizationPoses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Decks_DefaultLocalizationPoseId",
                table: "Decks",
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
                name: "FK_Decks_DefaultLocalizationPoses_DefaultLocalizationPoseId",
                table: "Decks",
                column: "DefaultLocalizationPoseId",
                principalTable: "DefaultLocalizationPoses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_DefaultLocalizationPoses_DefaultLocalizationPoseId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_DefaultLocalizationPoses_DefaultLocalizationPoseId",
                table: "Decks");

            migrationBuilder.DropTable(
                name: "DefaultLocalizationPoses");

            migrationBuilder.DropIndex(
                name: "IX_Decks_DefaultLocalizationPoseId",
                table: "Decks");

            migrationBuilder.DropIndex(
                name: "IX_Areas_DefaultLocalizationPoseId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPoseId",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationPoseId",
                table: "Areas");

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_W",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_X",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_Y",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Orientation_Z",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Position_X",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Position_Y",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DefaultLocalizationPose_Position_Z",
                table: "Areas",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
