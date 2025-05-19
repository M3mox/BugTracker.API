using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class UserBug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "Bugs");

            migrationBuilder.AddColumn<string>(
                name: "AssignedToId",
                table: "Bugs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Bugs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bugs_AssignedToId",
                table: "Bugs",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Bugs_CreatedById",
                table: "Bugs",
                column: "CreatedById");

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

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropIndex(
                name: "IX_Bugs_AssignedToId",
                table: "Bugs");

            migrationBuilder.DropIndex(
                name: "IX_Bugs_CreatedById",
                table: "Bugs");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "Bugs");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Bugs");

            migrationBuilder.AddColumn<string>(
                name: "AssignedTo",
                table: "Bugs",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
