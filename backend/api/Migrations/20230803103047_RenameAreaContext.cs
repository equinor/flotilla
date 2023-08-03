using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class RenameAreaContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_AssetDecks_CurrentAssetDeckId",
                table: "Robots");

            migrationBuilder.DropForeignKey(
                name: "FK_SafePositions_AssetDecks_AreaId",
                table: "SafePositions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AssetDecks",
                table: "AssetDecks");

            migrationBuilder.RenameTable(
                name: "AssetDecks",
                newName: "Areas");

            migrationBuilder.RenameIndex(
                name: "IX_AssetDecks_AssetCode_DeckName",
                table: "Areas",
                newName: "IX_Areas_AssetCode_DeckName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Areas",
                table: "Areas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Areas_CurrentAssetDeckId",
                table: "Robots",
                column: "CurrentAssetDeckId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SafePositions_Areas_AreaId",
                table: "SafePositions",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Areas_CurrentAssetDeckId",
                table: "Robots");

            migrationBuilder.DropForeignKey(
                name: "FK_SafePositions_Areas_AreaId",
                table: "SafePositions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Areas",
                table: "Areas");

            migrationBuilder.RenameTable(
                name: "Areas",
                newName: "AssetDecks");

            migrationBuilder.RenameIndex(
                name: "IX_Areas_AssetCode_DeckName",
                table: "AssetDecks",
                newName: "IX_AssetDecks_AssetCode_DeckName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssetDecks",
                table: "AssetDecks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_AssetDecks_CurrentAssetDeckId",
                table: "Robots",
                column: "CurrentAssetDeckId",
                principalTable: "AssetDecks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SafePositions_AssetDecks_AreaId",
                table: "SafePositions",
                column: "AreaId",
                principalTable: "AssetDecks",
                principalColumn: "Id");
        }
    }
}
