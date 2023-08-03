using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class RenameMissionContextName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Missions_Robots_RobotId",
                table: "Missions");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionTask_Missions_MissionRunId",
                table: "MissionTask");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Missions",
                table: "Missions");

            migrationBuilder.RenameTable(
                name: "Missions",
                newName: "MissionRuns");

            migrationBuilder.RenameIndex(
                name: "IX_Missions_RobotId",
                table: "MissionRuns",
                newName: "IX_MissionRuns_RobotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MissionRuns",
                table: "MissionRuns",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRuns_Robots_RobotId",
                table: "MissionRuns",
                column: "RobotId",
                principalTable: "Robots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTask_MissionRuns_MissionRunId",
                table: "MissionTask",
                column: "MissionRunId",
                principalTable: "MissionRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_Robots_RobotId",
                table: "MissionRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionTask_MissionRuns_MissionRunId",
                table: "MissionTask");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MissionRuns",
                table: "MissionRuns");

            migrationBuilder.RenameTable(
                name: "MissionRuns",
                newName: "Missions");

            migrationBuilder.RenameIndex(
                name: "IX_MissionRuns_RobotId",
                table: "Missions",
                newName: "IX_Missions_RobotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Missions",
                table: "Missions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Missions_Robots_RobotId",
                table: "Missions",
                column: "RobotId",
                principalTable: "Robots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTask_Missions_MissionRunId",
                table: "MissionTask",
                column: "MissionRunId",
                principalTable: "Missions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
