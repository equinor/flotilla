using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameEchoProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Sources");

            migrationBuilder.RenameColumn(
                name: "EchoTagLink",
                table: "MissionTasks",
                newName: "TagLink");

            migrationBuilder.RenameColumn(
                name: "EchoPoseId",
                table: "MissionTasks",
                newName: "PoseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TagLink",
                table: "MissionTasks",
                newName: "EchoTagLink");

            migrationBuilder.RenameColumn(
                name: "PoseId",
                table: "MissionTasks",
                newName: "EchoPoseId");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Sources",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
