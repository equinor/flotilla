using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAnalysisTypeToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisTypes",
                table: "Inspection");

            migrationBuilder.AddColumn<int>(
                name: "AnalysisType",
                table: "Inspection",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisType",
                table: "Inspection");

            migrationBuilder.AddColumn<string>(
                name: "AnalysisTypes",
                table: "Inspection",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }
    }
}
