using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAnalysisResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "InspectionUrl",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "TaskDescription",
                table: "Inspections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InspectionUrl",
                table: "Inspections",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskDescription",
                table: "Inspections",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnalysisResults",
                columns: table => new
                {
                    InspectionId = table.Column<string>(type: "text", nullable: false),
                    AnalysisGroupId = table.Column<string>(type: "text", nullable: true),
                    AnalysisType = table.Column<string>(type: "text", nullable: false),
                    BlobContainer = table.Column<string>(type: "text", nullable: true),
                    BlobName = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<float>(type: "real", nullable: true),
                    StorageAccount = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true),
                    Warning = table.Column<string>(type: "text", nullable: true)
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
    }
}
