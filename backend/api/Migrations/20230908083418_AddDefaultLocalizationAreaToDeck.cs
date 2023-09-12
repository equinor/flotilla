using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultLocalizationAreaToDeck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Decks_DeckId",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_DeckId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "DeckId",
                table: "Areas");

            migrationBuilder.AddColumn<string>(
                name: "AreaId",
                table: "Decks",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultLocalizationAreaId",
                table: "Decks",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Decks_AreaId",
                table: "Decks",
                column: "AreaId",
                unique: true,
                filter: "[AreaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Decks_DefaultLocalizationAreaId",
                table: "Decks",
                column: "DefaultLocalizationAreaId",
                unique: true,
                filter: "[DefaultLocalizationAreaId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_Areas_AreaId",
                table: "Decks",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_Areas_DefaultLocalizationAreaId",
                table: "Decks",
                column: "DefaultLocalizationAreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Decks_Areas_AreaId",
                table: "Decks");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_Areas_DefaultLocalizationAreaId",
                table: "Decks");

            migrationBuilder.DropIndex(
                name: "IX_Decks_AreaId",
                table: "Decks");

            migrationBuilder.DropIndex(
                name: "IX_Decks_DefaultLocalizationAreaId",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "DefaultLocalizationAreaId",
                table: "Decks");

            migrationBuilder.AddColumn<string>(
                name: "DeckId",
                table: "Areas",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Areas_DeckId",
                table: "Areas",
                column: "DeckId");

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
