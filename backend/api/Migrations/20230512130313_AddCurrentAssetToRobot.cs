using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class AddCurrentAssetToRobot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SafePosition_AssetDecks_AssetDeckId",
                table: "SafePosition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SafePosition",
                table: "SafePosition");

            migrationBuilder.RenameTable(
                name: "SafePosition",
                newName: "SafePositions");

            migrationBuilder.RenameIndex(
                name: "IX_SafePosition_AssetDeckId",
                table: "SafePositions",
                newName: "IX_SafePositions_AssetDeckId");

            migrationBuilder.AddColumn<string>(
                name: "CurrentAsset",
                table: "Robots",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SafePositions",
                table: "SafePositions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SafePositions_AssetDecks_AssetDeckId",
                table: "SafePositions",
                column: "AssetDeckId",
                principalTable: "AssetDecks",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SafePositions_AssetDecks_AssetDeckId",
                table: "SafePositions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SafePositions",
                table: "SafePositions");

            migrationBuilder.DropColumn(
                name: "CurrentAsset",
                table: "Robots");

            migrationBuilder.RenameTable(
                name: "SafePositions",
                newName: "SafePosition");

            migrationBuilder.RenameIndex(
                name: "IX_SafePositions_AssetDeckId",
                table: "SafePosition",
                newName: "IX_SafePosition_AssetDeckId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SafePosition",
                table: "SafePosition",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SafePosition_AssetDecks_AssetDeckId",
                table: "SafePosition",
                column: "AssetDeckId",
                principalTable: "AssetDecks",
                principalColumn: "Id");
        }
    }
}
