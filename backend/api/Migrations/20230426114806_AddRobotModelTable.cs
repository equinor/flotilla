using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class AddRobotModelTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Model", table: "Robots");

            migrationBuilder.AddColumn<string>(
                name: "ModelId",
                table: "Robots",
                type: "nvarchar(450)",
                nullable: true,
                defaultValue: ""
            );

            migrationBuilder.CreateTable(
                name: "RobotModels",
                columns: table =>
                    new
                    {
                        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        Type = table.Column<string>(type: "nvarchar(56)", nullable: false),
                        BatteryWarningThreshold = table.Column<float>(type: "real", nullable: true),
                        UpperPressureWarningThreshold = table.Column<float>(
                            type: "real",
                            nullable: true
                        ),
                        LowerPressureWarningThreshold = table.Column<float>(
                            type: "real",
                            nullable: true
                        )
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotModels", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Robots_ModelId",
                table: "Robots",
                column: "ModelId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RobotModels_Type",
                table: "RobotModels",
                column: "Type",
                unique: true
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_RobotModels_ModelId",
                table: "Robots",
                column: "ModelId",
                principalTable: "RobotModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Robots_RobotModels_ModelId", table: "Robots");

            migrationBuilder.DropTable(name: "RobotModels");

            migrationBuilder.DropIndex(name: "IX_Robots_ModelId", table: "Robots");

            migrationBuilder.DropColumn(name: "ModelId", table: "Robots");

            migrationBuilder.AddColumn<int>(
                name: "Model",
                table: "Robots",
                type: "int",
                nullable: false,
                defaultValue: 0
            );
        }
    }
}
