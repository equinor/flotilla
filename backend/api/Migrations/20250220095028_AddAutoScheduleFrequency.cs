using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoScheduleFrequency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "AutoScheduleFrequency_DaysOfWeek",
                table: "MissionDefinitions",
                type: "integer[]",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly[]>(
                name: "AutoScheduleFrequency_TimesOfDay",
                table: "MissionDefinitions",
                type: "time without time zone[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoScheduleFrequency_DaysOfWeek",
                table: "MissionDefinitions");

            migrationBuilder.DropColumn(
                name: "AutoScheduleFrequency_TimesOfDay",
                table: "MissionDefinitions");
        }
    }
}
