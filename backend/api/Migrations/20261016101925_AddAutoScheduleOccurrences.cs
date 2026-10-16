using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoScheduleOccurrences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoScheduleOccurrences",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MissionDefinitionId = table.Column<string>(type: "text", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    JobId = table.Column<string>(type: "text", nullable: true),
                    SchedulerInstance = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoScheduleOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoScheduleOccurrences_MissionDefinitions_MissionDefinitio~",
                        column: x => x.MissionDefinitionId,
                        principalTable: "MissionDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoScheduleOccurrences_MissionDefinitionId_ScheduledAt",
                table: "AutoScheduleOccurrences",
                columns: new[] { "MissionDefinitionId", "ScheduledAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoScheduleOccurrences");
        }
    }
}
