using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionFindingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InspectionFinding",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Area = table.Column<string>(type: "text", nullable: false),
                    IsarStepId = table.Column<string>(type: "text", nullable: false),
                    Findings = table.Column<string>(type: "text", nullable: false),
                    InspectionId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionFinding", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionFinding_Inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionFinding_InspectionId",
                table: "InspectionFinding",
                column: "InspectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspectionFinding");
        }
    }
}
