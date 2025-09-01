using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTelemetryFromRobotTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatteryLevel",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "BatteryState",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_W",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_X",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_Y",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Orientation_Z",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Position_X",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Position_Y",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Pose_Position_Z",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "PressureLevel",
                table: "Robots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "BatteryLevel",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "BatteryState",
                table: "Robots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_W",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_X",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_Y",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Orientation_Z",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Position_X",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Position_Y",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Pose_Position_Z",
                table: "Robots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "PressureLevel",
                table: "Robots",
                type: "real",
                nullable: true);
        }
    }
}
