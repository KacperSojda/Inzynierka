using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INZYNIERKA.Migrations
{
    /// <inheritdoc />
    public partial class GroupTags2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupTag_Groups_GroupId",
                table: "GroupTag");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupTag_Tags_TagId",
                table: "GroupTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupTag",
                table: "GroupTag");

            migrationBuilder.RenameTable(
                name: "GroupTag",
                newName: "GroupTags");

            migrationBuilder.RenameIndex(
                name: "IX_GroupTag_TagId",
                table: "GroupTags",
                newName: "IX_GroupTags_TagId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupTags",
                table: "GroupTags",
                columns: new[] { "GroupId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTags_Groups_GroupId",
                table: "GroupTags",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTags_Tags_TagId",
                table: "GroupTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupTags_Groups_GroupId",
                table: "GroupTags");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupTags_Tags_TagId",
                table: "GroupTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupTags",
                table: "GroupTags");

            migrationBuilder.RenameTable(
                name: "GroupTags",
                newName: "GroupTag");

            migrationBuilder.RenameIndex(
                name: "IX_GroupTags_TagId",
                table: "GroupTag",
                newName: "IX_GroupTag_TagId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupTag",
                table: "GroupTag",
                columns: new[] { "GroupId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTag_Groups_GroupId",
                table: "GroupTag",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTag_Tags_TagId",
                table: "GroupTag",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
