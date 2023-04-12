using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class AddAssetDeck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetDecks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeckName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_AssetDecks", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetDecks");
        }
    }
}
