using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledMissionRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_Robots_RobotId",
                table: "MissionRuns");

            migrationBuilder.AlterColumn<string>(
                name: "RobotId",
                table: "MissionRuns",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActivatedAt",
                table: "MissionRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleJobId",
                table: "MissionRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledAt",
                table: "MissionRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulerInstance",
                table: "MissionRuns",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionRuns_MissionId_ScheduledAt",
                table: "MissionRuns",
                columns: new[] { "MissionId", "ScheduledAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionRuns_Status_ScheduledAt",
                table: "MissionRuns",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_MissionRuns_RobotAssignment",
                table: "MissionRuns",
                sql: "\"RobotId\" IS NOT NULL OR (\"ScheduledAt\" IS NOT NULL AND \"ActivatedAt\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRuns_Robots_RobotId",
                table: "MissionRuns",
                column: "RobotId",
                principalTable: "Robots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old model cannot represent unactivated runs. Back up skips before rollback.
            migrationBuilder.Sql(
                """DELETE FROM "MissionRuns" WHERE "ScheduledAt" IS NOT NULL AND "ActivatedAt" IS NULL"""
            );
            migrationBuilder.DropForeignKey(
                name: "FK_MissionRuns_Robots_RobotId",
                table: "MissionRuns");

            migrationBuilder.DropIndex(
                name: "IX_MissionRuns_MissionId_ScheduledAt",
                table: "MissionRuns");

            migrationBuilder.DropIndex(
                name: "IX_MissionRuns_Status_ScheduledAt",
                table: "MissionRuns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MissionRuns_RobotAssignment",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "ScheduleJobId",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "SchedulerInstance",
                table: "MissionRuns");

            migrationBuilder.AlterColumn<string>(
                name: "RobotId",
                table: "MissionRuns",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionRuns_Robots_RobotId",
                table: "MissionRuns",
                column: "RobotId",
                principalTable: "Robots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
