using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeAreaInMissionsRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_Areas_AreaId",
                table: "MissionDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_Areas_AreaId",
                table: "MissionRuns");

            migrationBuilder.AlterColumn<string>(
                name: "AreaId",
                table: "MissionRuns",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AreaId",
                table: "MissionDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_Areas_AreaId",
                table: "MissionDefinitions",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRuns_Areas_AreaId",
                table: "MissionRuns",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_Areas_AreaId",
                table: "MissionDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_Areas_AreaId",
                table: "MissionRuns");

            migrationBuilder.AlterColumn<string>(
                name: "AreaId",
                table: "MissionRuns",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AreaId",
                table: "MissionDefinitions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_Areas_AreaId",
                table: "MissionDefinitions",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRuns_Areas_AreaId",
                table: "MissionRuns",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");
        }
    }
}
