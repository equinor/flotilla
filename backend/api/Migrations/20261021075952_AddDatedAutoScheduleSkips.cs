using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDatedAutoScheduleSkips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoScheduleFrequency_AutoScheduledJobs",
                table: "MissionDefinitions");

            migrationBuilder.CreateTable(
                name: "AutoScheduleSkips",
                columns: table => new
                {
                    MissionDefinitionId = table.Column<string>(type: "text", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoScheduleSkips", x => new { x.MissionDefinitionId, x.ScheduledAt });
                    table.ForeignKey(
                        name: "FK_AutoScheduleSkips_MissionDefinitions_MissionDefinitionId",
                        column: x => x.MissionDefinitionId,
                        principalTable: "MissionDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoScheduleSkips");

            migrationBuilder.AddColumn<string>(
                name: "AutoScheduleFrequency_AutoScheduledJobs",
                table: "MissionDefinitions",
                type: "text",
                nullable: true);
        }
    }
}
