using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDockPositionInsteadOfSafePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SafePositions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SafePositions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AreaId = table.Column<string>(type: "text", nullable: true),
                    Pose_Orientation_W = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Orientation_Z = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_X = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Y = table.Column<float>(type: "real", nullable: false),
                    Pose_Position_Z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafePositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafePositions_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SafePositions_AreaId",
                table: "SafePositions",
                column: "AreaId");
        }
    }
}
