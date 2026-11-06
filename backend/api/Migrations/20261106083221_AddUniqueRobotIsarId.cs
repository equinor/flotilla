using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueRobotIsarId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string keepRobot = """
                SELECT "Id", FIRST_VALUE("Id") OVER (
                    PARTITION BY "IsarId"
                    ORDER BY "Deprecated", "CurrentMissionId" IS NULL, "CurrentInspectionAreaId" IS NULL, "Id"
                ) AS "KeepId"
                FROM "Robots"
                """;

            migrationBuilder.Sql(
                $"""
                UPDATE "MissionRuns" AS m
                SET "RobotId" = d."KeepId"
                FROM ({keepRobot}) AS d
                WHERE m."RobotId" = d."Id" AND d."Id" <> d."KeepId";
                """
            );

            migrationBuilder.Sql(
                $"""
                DELETE FROM "Robots" AS r
                USING ({keepRobot}) AS d
                WHERE r."Id" = d."Id" AND d."Id" <> d."KeepId";
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_Robots_IsarId",
                table: "Robots",
                column: "IsarId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Robots_IsarId",
                table: "Robots");
        }
    }
}
