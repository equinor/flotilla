using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class RenameMissionToMissionRun : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionTask_Missions_MissionId",
                table: "MissionTask");

            migrationBuilder.RenameColumn(
                name: "MissionId",
                table: "MissionTask",
                newName: "MissionRunId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionTask_MissionId",
                table: "MissionTask",
                newName: "IX_MissionTask_MissionRunId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTask_Missions_MissionRunId",
                table: "MissionTask",
                column: "MissionRunId",
                principalTable: "Missions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionTask_Missions_MissionRunId",
                table: "MissionTask");

            migrationBuilder.RenameColumn(
                name: "MissionRunId",
                table: "MissionTask",
                newName: "MissionId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionTask_MissionRunId",
                table: "MissionTask",
                newName: "IX_MissionTask_MissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTask_Missions_MissionId",
                table: "MissionTask",
                column: "MissionId",
                principalTable: "Missions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
