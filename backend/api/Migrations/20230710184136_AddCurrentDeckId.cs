using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class AddCurrentDeckId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentAssetDeckId",
                table: "Robots",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Robots_CurrentAssetDeckId",
                table: "Robots",
                column: "CurrentAssetDeckId");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_AssetDecks_CurrentAssetDeckId",
                table: "Robots",
                column: "CurrentAssetDeckId",
                principalTable: "AssetDecks",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_AssetDecks_CurrentAssetDeckId",
                table: "Robots");

            migrationBuilder.DropIndex(
                name: "IX_Robots_CurrentAssetDeckId",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "CurrentAssetDeckId",
                table: "Robots");
        }
    }
}
