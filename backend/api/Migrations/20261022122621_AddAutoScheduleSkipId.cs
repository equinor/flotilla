using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoScheduleSkipId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AutoScheduleSkips",
                table: "AutoScheduleSkips");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "AutoScheduleSkips",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AutoScheduleSkips",
                table: "AutoScheduleSkips",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AutoScheduleSkips_MissionDefinitionId_ScheduledAt",
                table: "AutoScheduleSkips",
                columns: new[] { "MissionDefinitionId", "ScheduledAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AutoScheduleSkips",
                table: "AutoScheduleSkips");

            migrationBuilder.DropIndex(
                name: "IX_AutoScheduleSkips_MissionDefinitionId_ScheduledAt",
                table: "AutoScheduleSkips");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AutoScheduleSkips");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AutoScheduleSkips",
                table: "AutoScheduleSkips",
                columns: new[] { "MissionDefinitionId", "ScheduledAt" });
        }
    }
}
