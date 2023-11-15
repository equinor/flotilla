using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class inspectionfindingsupdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InspectionFindings",
                table: "InspectionFindings");

            migrationBuilder.RenameColumn(
                name: "RobotName",
                table: "InspectionFindings",
                newName: "IsarStepId");

            migrationBuilder.RenameColumn(
                name: "FindingsTag",
                table: "InspectionFindings",
                newName: "Findings");

            migrationBuilder.AlterColumn<DateTime>(
                name: "InspectionDate",
                table: "InspectionFindings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "InspectionFindings",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "InspectionId",
                table: "InspectionFindings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InspectionFindings",
                table: "InspectionFindings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionFindings_InspectionId",
                table: "InspectionFindings",
                column: "InspectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InspectionFindings",
                table: "InspectionFindings");

            migrationBuilder.DropIndex(
                name: "IX_InspectionFindings_InspectionId",
                table: "InspectionFindings");

            migrationBuilder.RenameColumn(
                name: "IsarStepId",
                table: "InspectionFindings",
                newName: "RobotName");

            migrationBuilder.RenameColumn(
                name: "Findings",
                table: "InspectionFindings",
                newName: "FindingsTag");

            migrationBuilder.AlterColumn<string>(
                name: "InspectionId",
                table: "InspectionFindings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InspectionDate",
                table: "InspectionFindings",
                type: "text",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "InspectionFindings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InspectionFindings",
                table: "InspectionFindings",
                columns: new[] { "InspectionId", "Id" });
        }
    }
}
