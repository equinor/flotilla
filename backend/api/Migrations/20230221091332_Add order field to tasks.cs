using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class Addorderfieldtotasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShouldRotate270Clockwise",
                table: "VideoStream",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PlanOrder",
                table: "PlannedTask",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShouldRotate270Clockwise",
                table: "VideoStream");

            migrationBuilder.DropColumn(
                name: "PlanOrder",
                table: "PlannedTask");
        }
    }
}
