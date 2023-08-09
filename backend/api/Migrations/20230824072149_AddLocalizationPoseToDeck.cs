using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizationPoseToDeck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalizationPoseId",
                table: "Decks",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalizationPoseId",
                table: "Areas",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocalizationPoses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
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
                    table.PrimaryKey("PK_LocalizationPoses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Decks_LocalizationPoseId",
                table: "Decks",
                column: "LocalizationPoseId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_LocalizationPoseId",
                table: "Areas",
                column: "LocalizationPoseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_LocalizationPoses_LocalizationPoseId",
                table: "Areas",
                column: "LocalizationPoseId",
                principalTable: "LocalizationPoses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_LocalizationPoses_LocalizationPoseId",
                table: "Decks",
                column: "LocalizationPoseId",
                principalTable: "LocalizationPoses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_LocalizationPoses_LocalizationPoseId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_LocalizationPoses_LocalizationPoseId",
                table: "Decks");

            migrationBuilder.DropTable(
                name: "LocalizationPoses");

            migrationBuilder.DropIndex(
                name: "IX_Decks_LocalizationPoseId",
                table: "Decks");

            migrationBuilder.DropIndex(
                name: "IX_Areas_LocalizationPoseId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "LocalizationPoseId",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "LocalizationPoseId",
                table: "Areas");
        }
    }
}
