using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class ModifyCurrentInspectionArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_InspectionAreas_CurrentInspectionAreaId",
                table: "Robots");

            migrationBuilder.DropIndex(
                name: "IX_Robots_CurrentInspectionAreaId",
                table: "Robots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Robots_CurrentInspectionAreaId",
                table: "Robots",
                column: "CurrentInspectionAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_InspectionAreas_CurrentInspectionAreaId",
                table: "Robots",
                column: "CurrentInspectionAreaId",
                principalTable: "InspectionAreas",
                principalColumn: "Id");
        }
    }
}
