using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class MakeRobotModelRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ModelId",
                table: "Robots",
                type: "nvarchar(450)",
                nullable: false,
                oldNullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ModelId",
                table: "Robots",
                type: "nvarchar(450)",
                nullable: true,
                oldNullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)"
            );
        }
    }
}
