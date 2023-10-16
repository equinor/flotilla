using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameLastRunToLastSuccessfulRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_MissionRuns_LastRunId",
                table: "MissionDefinitions");

            migrationBuilder.RenameColumn(
                name: "LastRunId",
                table: "MissionDefinitions",
                newName: "LastSuccessfulRunId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionDefinitions_LastRunId",
                table: "MissionDefinitions",
                newName: "IX_MissionDefinitions_LastSuccessfulRunId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_MissionRuns_LastSuccessfulRunId",
                table: "MissionDefinitions",
                column: "LastSuccessfulRunId",
                principalTable: "MissionRuns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_MissionRuns_LastSuccessfulRunId",
                table: "MissionDefinitions");

            migrationBuilder.RenameColumn(
                name: "LastSuccessfulRunId",
                table: "MissionDefinitions",
                newName: "LastRunId");

            migrationBuilder.RenameIndex(
                name: "IX_MissionDefinitions_LastSuccessfulRunId",
                table: "MissionDefinitions",
                newName: "IX_MissionDefinitions_LastRunId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_MissionRuns_LastRunId",
                table: "MissionDefinitions",
                column: "LastRunId",
                principalTable: "MissionRuns",
                principalColumn: "Id");
        }
    }
}
