using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookingApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "UserSavedRecipes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "UserSavedRecipes");
        }
    }
}
