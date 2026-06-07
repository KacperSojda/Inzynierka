using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INZYNIERKA.Migrations
{
    /// <inheritdoc />
    public partial class PoprawionoNazewnictwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SocialMedia",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Readed",
                table: "Messages",
                newName: "Read");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Read",
                table: "Messages",
                newName: "Readed");

            migrationBuilder.AddColumn<string>(
                name: "SocialMedia",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }
    }
}
