using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class RenameAssetDeckToArea : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SafePositions_AssetDecks_AssetDeckId",
                table: "SafePositions");

            migrationBuilder.RenameColumn(
                name: "AssetDeckId",
                table: "SafePositions",
                newName: "AreaId");

            migrationBuilder.RenameIndex(
                name: "IX_SafePositions_AssetDeckId",
                table: "SafePositions",
                newName: "IX_SafePositions_AreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_SafePositions_AssetDecks_AreaId",
                table: "SafePositions",
                column: "AreaId",
                principalTable: "AssetDecks",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SafePositions_AssetDecks_AreaId",
                table: "SafePositions");

            migrationBuilder.RenameColumn(
                name: "AreaId",
                table: "SafePositions",
                newName: "AssetDeckId");

            migrationBuilder.RenameIndex(
                name: "IX_SafePositions_AreaId",
                table: "SafePositions",
                newName: "IX_SafePositions_AssetDeckId");

            migrationBuilder.AddForeignKey(
                name: "FK_SafePositions_AssetDecks_AssetDeckId",
                table: "SafePositions",
                column: "AssetDeckId",
                principalTable: "AssetDecks",
                principalColumn: "Id");
        }
    }
}
