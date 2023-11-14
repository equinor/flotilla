using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionTargetToInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inspections_MissionTasks_MissionTaskId",
                table: "Inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionTasks_MissionRuns_MissionRunId",
                table: "MissionTasks");

            migrationBuilder.AlterColumn<string>(
                name: "MissionRunId",
                table: "MissionTasks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "MissionTaskId",
                table: "Inspections",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<float>(
                name: "InspectionTarget_X",
                table: "Inspections",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "InspectionTarget_Y",
                table: "Inspections",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "InspectionTarget_Z",
                table: "Inspections",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddForeignKey(
                name: "FK_Inspections_MissionTasks_MissionTaskId",
                table: "Inspections",
                column: "MissionTaskId",
                principalTable: "MissionTasks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTasks_MissionRuns_MissionRunId",
                table: "MissionTasks",
                column: "MissionRunId",
                principalTable: "MissionRuns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inspections_MissionTasks_MissionTaskId",
                table: "Inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionTasks_MissionRuns_MissionRunId",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "InspectionTarget_X",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "InspectionTarget_Y",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "InspectionTarget_Z",
                table: "Inspections");

            migrationBuilder.AlterColumn<string>(
                name: "MissionRunId",
                table: "MissionTasks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MissionTaskId",
                table: "Inspections",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Inspections_MissionTasks_MissionTaskId",
                table: "Inspections",
                column: "MissionTaskId",
                principalTable: "MissionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionTasks_MissionRuns_MissionRunId",
                table: "MissionTasks",
                column: "MissionRunId",
                principalTable: "MissionRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
