using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTimeseriesFunctionality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RobotBatteryTimeseries");

            migrationBuilder.DropTable(
                name: "RobotPoseTimeseries");

            migrationBuilder.DropTable(
                name: "RobotPressureTimeseries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RobotBatteryTimeseries",
                columns: table => new
                {
                    BatteryLevel = table.Column<float>(type: "real", nullable: false),
                    MissionId = table.Column<string>(type: "text", nullable: true),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "RobotPoseTimeseries",
                columns: table => new
                {
                    MissionId = table.Column<string>(type: "text", nullable: true),
                    OrientationW = table.Column<float>(type: "real", nullable: false),
                    OrientationX = table.Column<float>(type: "real", nullable: false),
                    OrientationY = table.Column<float>(type: "real", nullable: false),
                    OrientationZ = table.Column<float>(type: "real", nullable: false),
                    PositionX = table.Column<float>(type: "real", nullable: false),
                    PositionY = table.Column<float>(type: "real", nullable: false),
                    PositionZ = table.Column<float>(type: "real", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "RobotPressureTimeseries",
                columns: table => new
                {
                    MissionId = table.Column<string>(type: "text", nullable: true),
                    Pressure = table.Column<float>(type: "real", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                });
        }
    }
}
