using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExclusionArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExclusionAreas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PlantId = table.Column<string>(type: "text", nullable: false),
                    InstallationId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AreaPolygon_ZMin = table.Column<double>(type: "double precision", nullable: false),
                    AreaPolygon_ZMax = table.Column<double>(type: "double precision", nullable: false),
                    AreaPolygon_Positions = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExclusionAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExclusionAreas_Installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExclusionAreas_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExclusionAreas_InstallationId",
                table: "ExclusionAreas",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_ExclusionAreas_PlantId",
                table: "ExclusionAreas",
                column: "PlantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExclusionAreas");
        }
    }
}
