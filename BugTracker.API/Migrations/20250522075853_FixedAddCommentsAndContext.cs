using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class FixedAddCommentsAndContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bugs_User_AssignedToId",
                table: "Bugs");

            migrationBuilder.DropForeignKey(
                name: "FK_Bugs_User_CreatedById",
                table: "Bugs");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_User_CreatedById",
                table: "Comments");

            migrationBuilder.AddColumn<int>(
                name: "BugId1",
                table: "Comments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_BugId1",
                table: "Comments",
                column: "BugId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Bugs_User_AssignedToId",
                table: "Bugs",
                column: "AssignedToId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bugs_User_CreatedById",
                table: "Bugs",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Bugs_BugId1",
                table: "Comments",
                column: "BugId1",
                principalTable: "Bugs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_User_CreatedById",
                table: "Comments",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bugs_User_AssignedToId",
                table: "Bugs");

            migrationBuilder.DropForeignKey(
                name: "FK_Bugs_User_CreatedById",
                table: "Bugs");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Bugs_BugId1",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_User_CreatedById",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_BugId1",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "BugId1",
                table: "Comments");

            migrationBuilder.AddForeignKey(
                name: "FK_Bugs_User_AssignedToId",
                table: "Bugs",
                column: "AssignedToId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bugs_User_CreatedById",
                table: "Bugs",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_User_CreatedById",
                table: "Comments",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id");
        }
    }
}
