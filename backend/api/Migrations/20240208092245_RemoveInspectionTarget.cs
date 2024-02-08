using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInspectionTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InspectionTarget_X",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "InspectionTarget_Y",
                table: "MissionTasks");

            migrationBuilder.DropColumn(
                name: "InspectionTarget_Z",
                table: "MissionTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "InspectionTarget_X",
                table: "MissionTasks",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "InspectionTarget_Y",
                table: "MissionTasks",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "InspectionTarget_Z",
                table: "MissionTasks",
                type: "real",
                nullable: true);
        }
    }
}
