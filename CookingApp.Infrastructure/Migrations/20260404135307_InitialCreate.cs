using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CookingApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    MainIngredientName = table.Column<string>(type: "text", nullable: false),
                    FlavorTags = table.Column<string>(type: "text", nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: false),
                    PrepTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    CookTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngredientSubstitutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OriginalIngredientId = table.Column<int>(type: "integer", nullable: false),
                    SubstituteIngredientId = table.Column<int>(type: "integer", nullable: false),
                    ClosenessRank = table.Column<int>(type: "integer", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false),
                    DishImpact = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientSubstitutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredientSubstitutes_Ingredients_OriginalIngredientId",
                        column: x => x.OriginalIngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngredientSubstitutes_Ingredients_SubstituteIngredientId",
                        column: x => x.SubstituteIngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    IngredientId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<string>(type: "text", nullable: false),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => new { x.RecipeId, x.IngredientId });
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DislikedFlavors = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSavedRecipes",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSavedRecipes", x => new { x.UserId, x.RecipeId });
                    table.ForeignKey(
                        name: "FK_UserSavedRecipes_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSavedRecipes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDislikedIngredients",
                columns: table => new
                {
                    UserPreferencesId = table.Column<int>(type: "integer", nullable: false),
                    IngredientId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDislikedIngredients", x => new { x.UserPreferencesId, x.IngredientId });
                    table.ForeignKey(
                        name: "FK_UserDislikedIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserDislikedIngredients_UserPreferences_UserPreferencesId",
                        column: x => x.UserPreferencesId,
                        principalTable: "UserPreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Ingredients",
                columns: new[] { "Id", "Category", "Name", "Notes" },
                values: new object[,]
                {
                    { 1, "protein", "Chicken Breast", null },
                    { 2, "legume", "Chickpeas", null },
                    { 3, "vegetable", "Potato", null },
                    { 4, "herb", "Holy Basil", null },
                    { 5, "herb", "Thai Basil", null },
                    { 6, "herb", "Italian Basil", null },
                    { 7, "protein", "Egg", null },
                    { 8, "grain", "Breadcrumbs", null },
                    { 9, "vegetable", "Tomato", null },
                    { 10, "vegetable", "Onion", null },
                    { 11, "aromatic", "Garlic", null },
                    { 12, "aromatic", "Ginger", null },
                    { 13, "spice", "Cumin", null },
                    { 14, "spice", "Coriander", null },
                    { 15, "spice", "Garam Masala", null },
                    { 16, "condiment", "Fish Sauce", null },
                    { 17, "condiment", "Oyster Sauce", null },
                    { 18, "condiment", "Soy Sauce", null },
                    { 19, "spice", "Red Chili", null },
                    { 20, "grain", "Flour", null }
                });

            migrationBuilder.InsertData(
                table: "Recipes",
                columns: new[] { "Id", "Category", "CookTimeMinutes", "Country", "CreatedAt", "Description", "FlavorTags", "ImageUrl", "Instructions", "MainIngredientName", "Name", "PrepTimeMinutes", "Servings" },
                values: new object[,]
                {
                    { 1, "Main", 40, "India", new DateTime(2026, 4, 4, 13, 53, 6, 917, DateTimeKind.Utc).AddTicks(905), "A hearty North Indian chickpea curry fragrant with whole spices and tangy tomatoes.", "spicy,savory,tangy", "", "1. Soak chickpeas overnight and boil until tender.\n2. Fry onion until golden, add garlic and ginger paste.\n3. Add tomatoes and cook to a thick masala.\n4. Stir in cumin, coriander, garam masala and chickpeas.\n5. Simmer 20 minutes. Serve with rice or bhatura.", "Chickpeas", "Channa Masala", 20, 4 },
                    { 2, "Main", 10, "Austria", new DateTime(2026, 4, 4, 13, 53, 6, 917, DateTimeKind.Utc).AddTicks(913), "Crispy golden Austrian-style breaded chicken breast.", "savory,crispy", "", "1. Pound chicken breast to even thickness.\n2. Season with salt and pepper.\n3. Coat in flour, dip in beaten egg, then breadcrumbs.\n4. Fry in hot oil 3-4 min per side until golden.\n5. Drain and serve with lemon.", "Chicken Breast", "Chicken Schnitzel", 15, 2 },
                    { 3, "Breakfast", 15, "United States", new DateTime(2026, 4, 4, 13, 53, 6, 917, DateTimeKind.Utc).AddTicks(915), "Classic American crispy shredded potato patties.", "savory,crispy", "", "1. Grate potatoes and squeeze out all moisture.\n2. Season with salt and pepper.\n3. Press into patties and fry in butter on medium-high.\n4. Cook 4-5 min per side until deep golden brown.", "Potato", "Hash Browns", 10, 2 },
                    { 4, "Main", 8, "Thailand", new DateTime(2026, 4, 4, 13, 53, 6, 917, DateTimeKind.Utc).AddTicks(917), "Thai stir-fried minced chicken with holy basil — Thailand's most beloved street food.", "spicy,savory,aromatic", "", "1. Stir-fry garlic and chili in oil.\n2. Add minced chicken, cook through.\n3. Season with fish sauce, oyster sauce, soy sauce.\n4. Toss in holy basil off heat.\n5. Serve over jasmine rice with a fried egg.", "Chicken Breast", "Pad Kra Pao", 10, 2 }
                });

            migrationBuilder.InsertData(
                table: "IngredientSubstitutes",
                columns: new[] { "Id", "ClosenessRank", "DishImpact", "Explanation", "OriginalIngredientId", "SubstituteIngredientId" },
                values: new object[,]
                {
                    { 1, 2, "The dish will taste very similar but with a milder, less complex basil note.", "Thai basil is the closest substitute — it shares the same anise-like fragrance but is less peppery and slightly sweeter.", 4, 5 },
                    { 2, 3, "The dish will taste noticeably different; fresher and sweeter, without the signature spicy edge.", "Italian basil is a last resort — it lacks the peppery heat of holy basil entirely.", 4, 6 }
                });

            migrationBuilder.InsertData(
                table: "RecipeIngredients",
                columns: new[] { "IngredientId", "RecipeId", "IsOptional", "Quantity", "SortOrder" },
                values: new object[,]
                {
                    { 2, 1, false, "400g", 1 },
                    { 9, 1, false, "2 large", 2 },
                    { 10, 1, false, "1 large", 3 },
                    { 11, 1, false, "4 cloves", 4 },
                    { 12, 1, false, "1 inch", 5 },
                    { 13, 1, false, "1 tsp", 6 },
                    { 14, 1, false, "2 tsp", 7 },
                    { 15, 1, false, "1 tsp", 8 },
                    { 1, 2, false, "2 pieces", 1 },
                    { 7, 2, false, "2", 2 },
                    { 8, 2, false, "1 cup", 3 },
                    { 20, 2, false, "½ cup", 4 },
                    { 3, 3, false, "3 large", 1 },
                    { 1, 4, false, "300g minced", 1 },
                    { 4, 4, false, "1 cup", 2 },
                    { 11, 4, false, "5 cloves", 3 },
                    { 16, 4, false, "1 tbsp", 5 },
                    { 17, 4, false, "1 tbsp", 6 },
                    { 18, 4, false, "½ tbsp", 7 },
                    { 19, 4, false, "3", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientSubstitutes_OriginalIngredientId",
                table: "IngredientSubstitutes",
                column: "OriginalIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientSubstitutes_SubstituteIngredientId",
                table: "IngredientSubstitutes",
                column: "SubstituteIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Category",
                table: "Recipes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Country",
                table: "Recipes",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_MainIngredientName",
                table: "Recipes",
                column: "MainIngredientName");

            migrationBuilder.CreateIndex(
                name: "IX_UserDislikedIngredients_IngredientId",
                table: "UserDislikedIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSavedRecipes_RecipeId",
                table: "UserSavedRecipes",
                column: "RecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngredientSubstitutes");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "UserDislikedIngredients");

            migrationBuilder.DropTable(
                name: "UserSavedRecipes");

            migrationBuilder.DropTable(
                name: "Ingredients");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
