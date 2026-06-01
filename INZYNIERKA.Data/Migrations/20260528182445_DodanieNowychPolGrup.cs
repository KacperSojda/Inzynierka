using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INZYNIERKA.Migrations
{
    /// <inheritdoc />
    public partial class DodanieNowychPolGrup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Custom",
                table: "UserGroups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SmartReplies",
                table: "UserGroups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Tone",
                table: "UserGroups",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Custom",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "SmartReplies",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "Tone",
                table: "UserGroups");
        }
    }
}
