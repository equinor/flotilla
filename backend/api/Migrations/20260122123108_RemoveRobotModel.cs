using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRobotModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_RobotModels_ModelId",
                table: "Robots");

            migrationBuilder.DropTable(
                name: "RobotModels");

            migrationBuilder.DropIndex(
                name: "IX_Robots_ModelId",
                table: "Robots");

            migrationBuilder.RenameColumn(
                name: "ModelId",
                table: "Robots",
                newName: "Type");

            migrationBuilder.AddColumn<float>(
                name: "AverageDurationPerTag",
                table: "Robots",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageDurationPerTag",
                table: "Robots");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Robots",
                newName: "ModelId");

            migrationBuilder.CreateTable(
                name: "RobotModels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AverageDurationPerTag = table.Column<float>(type: "real", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Robots_ModelId",
                table: "Robots",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotModels_Type",
                table: "RobotModels",
                column: "Type",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_RobotModels_ModelId",
                table: "Robots",
                column: "ModelId",
                principalTable: "RobotModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
