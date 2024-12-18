using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameDeckToInspectionArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Decks_DeckId",
                table: "Areas");

            migrationBuilder.RenameColumn(
                name: "DeckId",
                table: "Areas",
                newName: "InspectionAreaId");

            migrationBuilder.RenameIndex(
                name: "IX_Areas_DeckId",
                table: "Areas",
                newName: "IX_Areas_InspectionAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Decks_InspectionAreaId",
                table: "Areas",
                column: "InspectionAreaId",
                principalTable: "Decks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Decks_InspectionAreaId",
                table: "Areas");

            migrationBuilder.RenameColumn(
                name: "InspectionAreaId",
                table: "Areas",
                newName: "DeckId");

            migrationBuilder.RenameIndex(
                name: "IX_Areas_InspectionAreaId",
                table: "Areas",
                newName: "IX_Areas_DeckId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Decks_DeckId",
                table: "Areas",
                column: "DeckId",
                principalTable: "Decks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
