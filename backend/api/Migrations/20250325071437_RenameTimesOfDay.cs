using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameTimesOfDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AutoScheduleFrequency_TimesOfDay",
                table: "MissionDefinitions",
                newName: "AutoScheduleFrequency_TimesOfDayCET");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AutoScheduleFrequency_TimesOfDayCET",
                table: "MissionDefinitions",
                newName: "AutoScheduleFrequency_TimesOfDay");
        }
    }
}
