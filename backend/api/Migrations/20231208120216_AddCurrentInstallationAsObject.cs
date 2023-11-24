using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentInstallationAsObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentInstallationId",
                table: "Robots",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Robots_CurrentInstallationId",
                table: "Robots",
                column: "CurrentInstallationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Installations_CurrentInstallationId",
                table: "Robots",
                column: "CurrentInstallationId",
                principalTable: "Installations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Installations_CurrentInstallationId",
                table: "Robots");

            migrationBuilder.DropIndex(
                name: "IX_Robots_CurrentInstallationId",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "CurrentInstallationId",
                table: "Robots");
        }
    }
}
