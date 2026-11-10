using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDatedAutoScheduling : Migration
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MissionDefinitionId = table.Column<string>(type: "text", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoScheduleSkips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoScheduleSkips_MissionDefinitions_MissionDefinitionId",
                        column: x => x.MissionDefinitionId,
                        principalTable: "MissionDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoScheduleSkips_MissionDefinitionId_ScheduledAt",
                table: "AutoScheduleSkips",
                columns: new[] { "MissionDefinitionId", "ScheduledAt" },
                unique: true);
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
