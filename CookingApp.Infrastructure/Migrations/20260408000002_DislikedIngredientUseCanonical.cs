using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookingApp.Infrastructure.Migrations;

public partial class DislikedIngredientUseCanonical : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop the old table (IngredientId FK) and recreate with CanonicalIngredientId
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""UserDislikedIngredients"";");

        migrationBuilder.Sql(@"
CREATE TABLE ""UserDislikedIngredients"" (
    ""UserPreferencesId"" integer NOT NULL,
    ""CanonicalIngredientId"" integer NOT NULL,
    CONSTRAINT ""PK_UserDislikedIngredients"" PRIMARY KEY (""UserPreferencesId"", ""CanonicalIngredientId""),
    CONSTRAINT ""FK_UserDislikedIngredients_UserPreferences_UserPreferencesId""
        FOREIGN KEY (""UserPreferencesId"") REFERENCES ""UserPreferences""(""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_UserDislikedIngredients_CanonicalIngredients_CanonicalIngredientId""
        FOREIGN KEY (""CanonicalIngredientId"") REFERENCES ""CanonicalIngredients""(""Id"") ON DELETE CASCADE
);");

        migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ""IX_UserDislikedIngredients_CanonicalIngredientId""
    ON ""UserDislikedIngredients""(""CanonicalIngredientId"");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""UserDislikedIngredients"";");

        migrationBuilder.Sql(@"
CREATE TABLE ""UserDislikedIngredients"" (
    ""UserPreferencesId"" integer NOT NULL,
    ""IngredientId"" integer NOT NULL,
    CONSTRAINT ""PK_UserDislikedIngredients"" PRIMARY KEY (""UserPreferencesId"", ""IngredientId""),
    CONSTRAINT ""FK_UserDislikedIngredients_UserPreferences_UserPreferencesId""
        FOREIGN KEY (""UserPreferencesId"") REFERENCES ""UserPreferences""(""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_UserDislikedIngredients_Ingredients_IngredientId""
        FOREIGN KEY (""IngredientId"") REFERENCES ""Ingredients""(""Id"") ON DELETE CASCADE
);");
    }
}
