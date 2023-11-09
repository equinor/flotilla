using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeInspectionNotOwned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionFindings_Inspection_InspectionId",
                table: "InspectionFindings");

            migrationBuilder.DropTable(
                name: "Inspection");

            migrationBuilder.CreateTable(
                name: "Inspections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IsarStepId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    InspectionType = table.Column<string>(type: "text", nullable: false),
                    VideoDuration = table.Column<float>(type: "real", nullable: true),
                    AnalysisType = table.Column<string>(type: "text", nullable: true),
                    InspectionUrl = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MissionTaskId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspections_MissionTask_MissionTaskId",
                        column: x => x.MissionTaskId,
                        principalTable: "MissionTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_MissionTaskId",
                table: "Inspections",
                column: "MissionTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionFindings_Inspections_InspectionId",
                table: "InspectionFindings",
                column: "InspectionId",
                principalTable: "Inspections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionFindings_Inspections_InspectionId",
                table: "InspectionFindings");

            migrationBuilder.DropTable(
                name: "Inspections");

            migrationBuilder.CreateTable(
                name: "Inspection",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AnalysisType = table.Column<string>(type: "text", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InspectionType = table.Column<string>(type: "text", nullable: false),
                    InspectionUrl = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsarStepId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MissionTaskId = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    VideoDuration = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspection_MissionTask_MissionTaskId",
                        column: x => x.MissionTaskId,
                        principalTable: "MissionTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_MissionTaskId",
                table: "Inspection",
                column: "MissionTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionFindings_Inspection_InspectionId",
                table: "InspectionFindings",
                column: "InspectionId",
                principalTable: "Inspection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
