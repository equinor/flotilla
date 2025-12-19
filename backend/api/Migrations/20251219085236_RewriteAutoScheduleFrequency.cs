using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RewriteAutoScheduleFrequency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoScheduleFrequency_DaysOfWeek",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "AutoScheduleFrequency_TimesOfDayCET",
                table: "MissionDefinitions");
            
            migrationBuilder.DropColumn(
                name: "AutoScheduleFrequency_AutoScheduledJobs",
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
                    DayOfWeek = table.Column<string>(type: "text", nullable: false),
                    TimeOfDay = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    AutoScheduleFrequencyId = table.Column<string>(type: "text", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissionDefinitions_AutoScheduleFrequency_AutoScheduleFreque~",
                table: "MissionDefinitions");

            migrationBuilder.DropTable(
                name: "TimeAndDay");

            migrationBuilder.DropTable(
                name: "AutoScheduleFrequency");

            migrationBuilder.DropIndex(
                name: "IX_MissionDefinitions_AutoScheduleFrequencyId",
                table: "MissionDefinitions");

            migrationBuilder.AddColumn<int[]>(
                name: "AutoScheduleFrequency_DaysOfWeek",
                table: "MissionDefinitions",
                type: "integer[]",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly[]>(
                name: "AutoScheduleFrequency_TimesOfDayCET",
                table: "MissionDefinitions",
                type: "time without time zone[]",
                nullable: true);
            
            migrationBuilder.AddColumn<string>(
                name: "AutoScheduleFrequency_AutoScheduledJobs",
                table: "MissionDefinitions",
                type: "text",
                nullable: true);
            
            migrationBuilder.DropColumn(
                name: "AutoScheduleFrequencyId",
                table: "MissionDefinitions");
        }
    }
}
