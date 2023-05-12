using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class ChangeEstimatedDurationToInt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EstimatedDuration_BigInt",
                table: "Missions",
                nullable: true
            );

            migrationBuilder.Sql(
                "UPDATE Missions SET EstimatedDuration_BigInt = DATEDIFF(SECOND, '00:00:00', EstimatedDuration)"
            );

            migrationBuilder.DropColumn(name: "EstimatedDuration", table: "Missions");

            migrationBuilder.RenameColumn(
                name: "EstimatedDuration_BigInt",
                table: "Missions",
                newName: "EstimatedDuration"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "EstimatedDuration_TimeSpan",
                type: "time",
                table: "Missions",
                nullable: true
            );

            migrationBuilder.Sql(
                "UPDATE Missions SET EstimatedDuration_TimeSpan = CONVERT(TIME, DATEADD(SECOND, EstimatedDuration, '00:00:00'))"
            );

            migrationBuilder.DropColumn(name: "EstimatedDuration", table: "Missions");

            migrationBuilder.RenameColumn(
                name: "EstimatedDuration_TimeSpan",
                table: "Missions",
                newName: "EstimatedDuration"
            );
        }
    }
}
