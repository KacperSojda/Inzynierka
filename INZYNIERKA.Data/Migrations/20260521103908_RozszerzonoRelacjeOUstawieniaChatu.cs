using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INZYNIERKA.Migrations
{
    /// <inheritdoc />
    public partial class RozszerzonoRelacjeOUstawieniaChatu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Custom",
                table: "UserFriends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SmartReplies",
                table: "UserFriends",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Tone",
                table: "UserFriends",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Custom",
                table: "UserFriends");

            migrationBuilder.DropColumn(
                name: "SmartReplies",
                table: "UserFriends");

            migrationBuilder.DropColumn(
                name: "Tone",
                table: "UserFriends");
        }
    }
}
