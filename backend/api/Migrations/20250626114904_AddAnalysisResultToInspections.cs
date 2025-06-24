using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisResultToInspections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisResults",
                columns: table => new
                {
                    InspectionId = table.Column<string>(type: "text", nullable: false),
                    AnalysisType = table.Column<string>(type: "text", nullable: false),
                    DisplayText = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<float>(type: "real", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    Class = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<float>(type: "real", nullable: true),
                    Warning = table.Column<string>(type: "text", nullable: true),
                    StorageAccount = table.Column<string>(type: "text", nullable: false),
                    BlobContainer = table.Column<string>(type: "text", nullable: false),
                    BlobName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisResults", x => x.InspectionId);
                    table.ForeignKey(
                        name: "FK_AnalysisResults_Inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisResults");
        }
    }
}
