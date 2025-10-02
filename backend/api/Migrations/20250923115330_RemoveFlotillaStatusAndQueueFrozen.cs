using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFlotillaStatusAndQueueFrozen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlotillaStatus",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "MissionQueueFrozen",
                table: "Robots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlotillaStatus",
                table: "Robots",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MissionQueueFrozen",
                table: "Robots",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
