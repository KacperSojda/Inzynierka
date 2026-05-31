using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INZYNIERKA.Migrations
{
    /// <inheritdoc />
    public partial class PopawionoNazewnictwoDanych : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreationDate",
                table: "Notifications",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "IsRead",
                table: "Messages",
                newName: "Readed");

            migrationBuilder.RenameColumn(
                name: "DateTime",
                table: "Messages",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "SocialMediaUrl",
                table: "AspNetUsers",
                newName: "SocialMedia");

            migrationBuilder.RenameColumn(
                name: "LastActiveDate",
                table: "AspNetUsers",
                newName: "LastActive");

            migrationBuilder.RenameColumn(
                name: "DateOfBirth",
                table: "AspNetUsers",
                newName: "BirthDate");

            migrationBuilder.RenameColumn(
                name: "CustomStatus",
                table: "AspNetUsers",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "CoverPhoto",
                table: "AspNetUsers",
                newName: "Cover");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "Notifications",
                newName: "CreationDate");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "Messages",
                newName: "DateTime");

            migrationBuilder.RenameColumn(
                name: "Readed",
                table: "Messages",
                newName: "IsRead");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "AspNetUsers",
                newName: "CustomStatus");

            migrationBuilder.RenameColumn(
                name: "SocialMedia",
                table: "AspNetUsers",
                newName: "SocialMediaUrl");

            migrationBuilder.RenameColumn(
                name: "LastActive",
                table: "AspNetUsers",
                newName: "LastActiveDate");

            migrationBuilder.RenameColumn(
                name: "Cover",
                table: "AspNetUsers",
                newName: "CoverPhoto");

            migrationBuilder.RenameColumn(
                name: "BirthDate",
                table: "AspNetUsers",
                newName: "DateOfBirth");
        }
    }
}
