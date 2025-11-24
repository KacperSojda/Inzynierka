using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INZYNIERKA.Migrations
{
    /// <inheritdoc />
    public partial class NotificationUpdate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GroupId",
                table: "Notifications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "GroupId1",
                table: "Notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_GroupId1",
                table: "Notifications",
                column: "GroupId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Groups_GroupId1",
                table: "Notifications",
                column: "GroupId1",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Groups_GroupId1",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_GroupId1",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "GroupId1",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "GroupId",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
