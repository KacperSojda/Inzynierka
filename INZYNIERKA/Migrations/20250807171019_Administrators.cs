using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INZYNIERKA.Migrations
{
    /// <inheritdoc />
    public partial class Administrators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "UserGroups",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "UserGroups");
        }
    }
}
