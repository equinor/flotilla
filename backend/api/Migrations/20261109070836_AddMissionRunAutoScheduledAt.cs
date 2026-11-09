using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionRunAutoScheduledAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AutoScheduledAt",
                table: "MissionRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionRuns_MissionId_AutoScheduledAt",
                table: "MissionRuns",
                columns: new[] { "MissionId", "AutoScheduledAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MissionRuns_MissionId_AutoScheduledAt",
                table: "MissionRuns");

            migrationBuilder.DropColumn(
                name: "AutoScheduledAt",
                table: "MissionRuns");
        }
    }
}
