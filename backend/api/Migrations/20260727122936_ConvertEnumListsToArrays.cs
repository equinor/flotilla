using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertEnumListsToArrays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Raw SQL needed: USING clause converts ';'-delimited strings to text arrays
            migrationBuilder.Sql(
                """
                ALTER TABLE "Inspections"
                    ALTER COLUMN "AnalysisTypes" TYPE text[]
                    USING CASE
                        WHEN "AnalysisTypes" IS NULL OR "AnalysisTypes" = '' THEN NULL
                        ELSE string_to_array("AnalysisTypes", ';')
                    END;
                """
            );

            migrationBuilder.Sql(
                """
                ALTER TABLE "MissionTasks"
                    ALTER COLUMN "AnalysisTypes" TYPE text[]
                    USING CASE
                        WHEN "AnalysisTypes" IS NULL OR "AnalysisTypes" = '' THEN NULL
                        ELSE string_to_array("AnalysisTypes", ';')
                    END;
                """
            );

            migrationBuilder.Sql(
                """
                ALTER TABLE "Robots"
                    ALTER COLUMN "RobotCapabilities" TYPE text[]
                    USING CASE
                        WHEN "RobotCapabilities" IS NULL OR "RobotCapabilities" = '' THEN NULL
                        ELSE string_to_array("RobotCapabilities", ';')
                    END;
                """
            );

            // NOT NULL column; empty list was stored as empty string
            migrationBuilder.Sql(
                """
                ALTER TABLE "TaskDefinition"
                    ALTER COLUMN "AnalysisTypes" TYPE text[]
                    USING CASE
                        WHEN "AnalysisTypes" = '' THEN ARRAY[]::text[]
                        ELSE string_to_array("AnalysisTypes", ';')
                    END;
                """
            );

            // AlterColumn calls give EF structural awareness of the type change
            migrationBuilder.AlterColumn<string[]>(
                name: "AnalysisTypes",
                table: "TaskDefinition",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string[]>(
                name: "RobotCapabilities",
                table: "Robots",
                type: "text[]",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string[]>(
                name: "AnalysisTypes",
                table: "MissionTasks",
                type: "text[]",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string[]>(
                name: "AnalysisTypes",
                table: "Inspections",
                type: "text[]",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // AlterColumn first so EF records the revert before the SQL runs
            migrationBuilder.AlterColumn<string>(
                name: "AnalysisTypes",
                table: "TaskDefinition",
                type: "text",
                nullable: false,
                oldClrType: typeof(string[]),
                oldType: "text[]"
            );

            migrationBuilder.AlterColumn<string>(
                name: "RobotCapabilities",
                table: "Robots",
                type: "text",
                nullable: true,
                oldClrType: typeof(string[]),
                oldType: "text[]",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "AnalysisTypes",
                table: "MissionTasks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string[]),
                oldType: "text[]",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "AnalysisTypes",
                table: "Inspections",
                type: "text",
                nullable: true,
                oldClrType: typeof(string[]),
                oldType: "text[]",
                oldNullable: true
            );

            // Raw SQL needed: USING clause joins array elements back to ';'-delimited text
            migrationBuilder.Sql(
                """
                ALTER TABLE "Inspections"
                    ALTER COLUMN "AnalysisTypes" TYPE text
                    USING CASE
                        WHEN "AnalysisTypes" IS NULL THEN NULL
                        ELSE array_to_string("AnalysisTypes", ';')
                    END;
                """
            );

            migrationBuilder.Sql(
                """
                ALTER TABLE "MissionTasks"
                    ALTER COLUMN "AnalysisTypes" TYPE text
                    USING CASE
                        WHEN "AnalysisTypes" IS NULL THEN NULL
                        ELSE array_to_string("AnalysisTypes", ';')
                    END;
                """
            );

            migrationBuilder.Sql(
                """
                ALTER TABLE "Robots"
                    ALTER COLUMN "RobotCapabilities" TYPE text
                    USING CASE
                        WHEN "RobotCapabilities" IS NULL THEN NULL
                        ELSE array_to_string("RobotCapabilities", ';')
                    END;
                """
            );

            migrationBuilder.Sql(
                """
                ALTER TABLE "TaskDefinition"
                    ALTER COLUMN "AnalysisTypes" TYPE text
                    USING array_to_string("AnalysisTypes", ';');
                """
            );
        }
    }
}
