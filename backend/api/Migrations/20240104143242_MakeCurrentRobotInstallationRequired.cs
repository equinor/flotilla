using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeCurrentRobotInstallationRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Installations_CurrentInstallationId",
                table: "Robots");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentInstallationId",
                table: "Robots",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Installations_CurrentInstallationId",
                table: "Robots",
                column: "CurrentInstallationId",
                principalTable: "Installations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Installations_CurrentInstallationId",
                table: "Robots");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentInstallationId",
                table: "Robots",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Installations_CurrentInstallationId",
                table: "Robots",
                column: "CurrentInstallationId",
                principalTable: "Installations",
                principalColumn: "Id");
        }
    }
}
