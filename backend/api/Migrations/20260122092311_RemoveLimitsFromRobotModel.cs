using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLimitsFromRobotModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatteryMissionStartThreshold",
                table: "RobotModels");

            migrationBuilder.DropColumn(
                name: "BatteryWarningThreshold",
                table: "RobotModels");

            migrationBuilder.DropColumn(
                name: "LowerPressureWarningThreshold",
                table: "RobotModels");

            migrationBuilder.DropColumn(
                name: "UpperPressureWarningThreshold",
                table: "RobotModels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "BatteryMissionStartThreshold",
                table: "RobotModels",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "BatteryWarningThreshold",
                table: "RobotModels",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "LowerPressureWarningThreshold",
                table: "RobotModels",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "UpperPressureWarningThreshold",
                table: "RobotModels",
                type: "real",
                nullable: true);
        }
    }
}
