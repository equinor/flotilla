using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyAutoScheduleFrequency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutoScheduleFrequency_SchedulingTimesCETperWeek",
                table: "MissionDefinitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoScheduleFrequency_AutoScheduledJobs",
                table: "MissionDefinitions",
                type: "text",
                nullable: true);

            // Move existing schedules into the owned JSON column before dropping the old tables.
            // DayOfWeek is stored as text but must be serialized as its numeric value to match the
            // System.Text.Json value converter; idle rows with no times are intentionally left null.
            migrationBuilder.Sql(
                """
                UPDATE "MissionDefinitions" md
                SET "AutoScheduleFrequency_SchedulingTimesCETperWeek" = sub.times,
                    "AutoScheduleFrequency_AutoScheduledJobs" = asf."AutoScheduledJobs"
                FROM "AutoScheduleFrequency" asf
                JOIN (
                    SELECT tad."AutoScheduleFrequencyId" AS asf_id,
                           json_agg(
                               json_build_object(
                                   'DayOfWeek',
                                   CASE tad."DayOfWeek"
                                       WHEN 'Sunday' THEN 0
                                       WHEN 'Monday' THEN 1
                                       WHEN 'Tuesday' THEN 2
                                       WHEN 'Wednesday' THEN 3
                                       WHEN 'Thursday' THEN 4
                                       WHEN 'Friday' THEN 5
                                       WHEN 'Saturday' THEN 6
                                   END,
                                   'TimeOfDay', to_char(tad."TimeOfDay", 'HH24:MI:SS')
                               )
                           )::text AS times
                    FROM "TimeAndDay" tad
                    GROUP BY tad."AutoScheduleFrequencyId"
                ) sub ON sub.asf_id = asf."Id"
                WHERE md."AutoScheduleFrequencyId" = asf."Id";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_AutoScheduleFrequency_AutoScheduleFreque~",
                table: "MissionDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_MissionDefinitions_AutoScheduleFrequencyId",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "AutoScheduleFrequencyId",
                table: "MissionDefinitions");

            migrationBuilder.DropTable(
                name: "TimeAndDay");

            migrationBuilder.DropTable(
                name: "AutoScheduleFrequency");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoScheduleFrequency_AutoScheduledJobs",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "AutoScheduleFrequency_SchedulingTimesCETperWeek",
                table: "MissionDefinitions");

            migrationBuilder.AddColumn<string>(
                name: "AutoScheduleFrequencyId",
                table: "MissionDefinitions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AutoScheduleFrequency",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AutoScheduledJobs = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoScheduleFrequency", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeAndDay",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AutoScheduleFrequencyId = table.Column<string>(type: "text", nullable: false),
                    DayOfWeek = table.Column<string>(type: "text", nullable: false),
                    TimeOfDay = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeAndDay", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeAndDay_AutoScheduleFrequency_AutoScheduleFrequencyId",
                        column: x => x.AutoScheduleFrequencyId,
                        principalTable: "AutoScheduleFrequency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionDefinitions_AutoScheduleFrequencyId",
                table: "MissionDefinitions",
                column: "AutoScheduleFrequencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeAndDay_AutoScheduleFrequencyId",
                table: "TimeAndDay",
                column: "AutoScheduleFrequencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_MissionDefinitions_AutoScheduleFrequency_AutoScheduleFreque~",
                table: "MissionDefinitions",
                column: "AutoScheduleFrequencyId",
                principalTable: "AutoScheduleFrequency",
                principalColumn: "Id");
        }
    }
}
