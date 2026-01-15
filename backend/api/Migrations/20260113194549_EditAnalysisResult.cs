using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class EditAnalysisResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Class",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "DisplayText",
                table: "AnalysisResults");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "AnalysisResults",
                type: "text",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Value",
                table: "AnalysisResults",
                type: "real",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Class",
                table: "AnalysisResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayText",
                table: "AnalysisResults",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
