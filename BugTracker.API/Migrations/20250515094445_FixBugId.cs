using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class FixBugId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Bugs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Bugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
