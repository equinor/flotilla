using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsarConnectedAndDisconnectTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisconnectTime",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "IsarConnected",
                table: "Robots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisconnectTime",
                table: "Robots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsarConnected",
                table: "Robots",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
