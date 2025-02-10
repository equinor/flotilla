using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Areas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    InspectionAreaId = table.Column<string>(type: "text", nullable: false),
                    InstallationId = table.Column<string>(type: "text", nullable: false),
                    PlantId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MapMetadata_MapName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MapMetadata_Boundary_X1 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_Boundary_X2 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_Boundary_Y1 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_Boundary_Y2 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_Boundary_Z1 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_Boundary_Z2 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_TransformationMatrices_C1 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_TransformationMatrices_C2 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_TransformationMatrices_D1 = table.Column<double>(type: "double precision", nullable: false),
                    MapMetadata_TransformationMatrices_D2 = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Areas_InspectionAreas_InspectionAreaId",
                        column: x => x.InspectionAreaId,
                        principalTable: "InspectionAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Areas_Installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Areas_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_InspectionAreaId",
                table: "Areas",
                column: "InspectionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_InstallationId",
                table: "Areas",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_PlantId",
                table: "Areas",
                column: "PlantId");
        }
    }
}
