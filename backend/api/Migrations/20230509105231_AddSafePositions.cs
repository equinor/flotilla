using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class AddSafePositions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SafePosition",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Pose_Position_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    AssetDeckId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafePosition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafePosition_AssetDecks_AssetDeckId",
                        column: x => x.AssetDeckId,
                        principalTable: "AssetDecks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetDecks_AssetCode_DeckName",
                table: "AssetDecks",
                columns: new[] { "AssetCode", "DeckName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SafePosition_AssetDeckId",
                table: "SafePosition",
                column: "AssetDeckId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SafePosition");

            migrationBuilder.DropIndex(
                name: "IX_AssetDecks_AssetCode_DeckName",
                table: "AssetDecks");
        }
    }
}
