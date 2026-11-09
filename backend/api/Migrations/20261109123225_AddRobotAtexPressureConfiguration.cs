using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRobotAtexPressureConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAtex",
                table: "Robots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<float>(
                name: "LowerPressureWarningThreshold",
                table: "Robots",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "UpperPressureWarningThreshold",
                table: "Robots",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAtex",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "LowerPressureWarningThreshold",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "UpperPressureWarningThreshold",
                table: "Robots");
        }
    }
}
