using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookingApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalIngredientCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Column may already exist in the DB — only add if missing
            migrationBuilder.Sql(@"
                ALTER TABLE ""CanonicalIngredients""
                ADD COLUMN IF NOT EXISTS ""Category"" text NOT NULL DEFAULT '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""CanonicalIngredients""
                DROP COLUMN IF EXISTS ""Category"";
            ");
        }
    }
}
