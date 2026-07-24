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
            // Convert semicolon-delimited text columns to native text[] arrays.
            // Rows with NULL or empty string become NULL or empty array respectively.

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

            // TaskDefinition.AnalysisTypes is NOT NULL; empty list was stored as empty string.
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert text[] arrays back to semicolon-delimited text columns.
            // NULL arrays become NULL; empty arrays become empty string.

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
