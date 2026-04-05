using CookingApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CookingApp.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<IngredientSubstitute> IngredientSubstitutes => Set<IngredientSubstitute>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<UserDislikedIngredient> UserDislikedIngredients => Set<UserDislikedIngredient>();
    public DbSet<UserSavedRecipe> UserSavedRecipes => Set<UserSavedRecipe>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ── RecipeIngredient composite key ────────────────────────────────────
        b.Entity<RecipeIngredient>()
            .HasKey(ri => new { ri.RecipeId, ri.IngredientId });

        b.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Recipe)
            .WithMany(r => r.RecipeIngredients)
            .HasForeignKey(ri => ri.RecipeId);

        b.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Ingredient)
            .WithMany(i => i.RecipeIngredients)
            .HasForeignKey(ri => ri.IngredientId);

        // ── IngredientSubstitute ──────────────────────────────────────────────
        b.Entity<IngredientSubstitute>()
            .HasOne(s => s.OriginalIngredient)
            .WithMany(i => i.Substitutes)
            .HasForeignKey(s => s.OriginalIngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<IngredientSubstitute>()
            .HasOne(s => s.SubstituteIngredient)
            .WithMany()
            .HasForeignKey(s => s.SubstituteIngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── UserPreferences 1-to-1 ────────────────────────────────────────────
        b.Entity<UserPreferences>()
            .HasOne(p => p.User)
            .WithOne(u => u.Preferences)
            .HasForeignKey<UserPreferences>(p => p.UserId);

        // ── UserDislikedIngredient composite key ──────────────────────────────
        b.Entity<UserDislikedIngredient>()
            .HasKey(d => new { d.UserPreferencesId, d.IngredientId });

        b.Entity<UserDislikedIngredient>()
            .HasOne(d => d.UserPreferences)
            .WithMany(p => p.DislikedIngredients)
            .HasForeignKey(d => d.UserPreferencesId);

        b.Entity<UserDislikedIngredient>()
            .HasOne(d => d.Ingredient)
            .WithMany()
            .HasForeignKey(d => d.IngredientId);

        // ── UserSavedRecipe composite key ─────────────────────────────────────
        b.Entity<UserSavedRecipe>()
            .HasKey(s => new { s.UserId, s.RecipeId });

        b.Entity<UserSavedRecipe>()
            .HasOne(s => s.User)
            .WithMany(u => u.SavedRecipes)
            .HasForeignKey(s => s.UserId);

        b.Entity<UserSavedRecipe>()
            .HasOne(s => s.Recipe)
            .WithMany(r => r.SavedByUsers)
            .HasForeignKey(s => s.RecipeId);

        // ── Indexes ───────────────────────────────────────────────────────────
        b.Entity<Recipe>().HasIndex(r => r.Country);
        b.Entity<Recipe>().HasIndex(r => r.Category);
        b.Entity<Recipe>().HasIndex(r => r.MainIngredientName);
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // ── Seed data ─────────────────────────────────────────────────────────
        SeedData(b);
    }

    private static void SeedData(ModelBuilder b)
    {
        // Ingredients
        b.Entity<Ingredient>().HasData(
            new Ingredient { Id = 1,  Name = "Chicken Breast",    Category = "protein" },
            new Ingredient { Id = 2,  Name = "Chickpeas",         Category = "legume" },
            new Ingredient { Id = 3,  Name = "Potato",            Category = "vegetable" },
            new Ingredient { Id = 4,  Name = "Holy Basil",        Category = "herb" },
            new Ingredient { Id = 5,  Name = "Thai Basil",        Category = "herb" },
            new Ingredient { Id = 6,  Name = "Italian Basil",     Category = "herb" },
            new Ingredient { Id = 7,  Name = "Egg",               Category = "protein" },
            new Ingredient { Id = 8,  Name = "Breadcrumbs",       Category = "grain" },
            new Ingredient { Id = 9,  Name = "Tomato",            Category = "vegetable" },
            new Ingredient { Id = 10, Name = "Onion",             Category = "vegetable" },
            new Ingredient { Id = 11, Name = "Garlic",            Category = "aromatic" },
            new Ingredient { Id = 12, Name = "Ginger",            Category = "aromatic" },
            new Ingredient { Id = 13, Name = "Cumin",             Category = "spice" },
            new Ingredient { Id = 14, Name = "Coriander",         Category = "spice" },
            new Ingredient { Id = 15, Name = "Garam Masala",      Category = "spice" },
            new Ingredient { Id = 16, Name = "Fish Sauce",        Category = "condiment" },
            new Ingredient { Id = 17, Name = "Oyster Sauce",      Category = "condiment" },
            new Ingredient { Id = 18, Name = "Soy Sauce",         Category = "condiment" },
            new Ingredient { Id = 19, Name = "Red Chili",         Category = "spice" },
            new Ingredient { Id = 20, Name = "Flour",             Category = "grain" }
        );

        // Substitutes: Holy Basil → Thai Basil (rank 2) → Italian Basil (rank 3)
        b.Entity<IngredientSubstitute>().HasData(
            new IngredientSubstitute
            {
                Id = 1,
                OriginalIngredientId = 4,    // Holy Basil
                SubstituteIngredientId = 5,  // Thai Basil
                ClosenessRank = 2,
                Explanation = "Thai basil is the closest substitute — it shares the same anise-like fragrance but is less peppery and slightly sweeter.",
                DishImpact = "The dish will taste very similar but with a milder, less complex basil note."
            },
            new IngredientSubstitute
            {
                Id = 2,
                OriginalIngredientId = 4,    // Holy Basil
                SubstituteIngredientId = 6,  // Italian Basil
                ClosenessRank = 3,
                Explanation = "Italian basil is a last resort — it lacks the peppery heat of holy basil entirely.",
                DishImpact = "The dish will taste noticeably different; fresher and sweeter, without the signature spicy edge."
            }
        );

        // Recipes
        b.Entity<Recipe>().HasData(
            new Recipe
            {
                Id = 1,
                Name = "Channa Masala",
                Description = "A hearty North Indian chickpea curry fragrant with whole spices and tangy tomatoes.",
                Country = "India",
                Category = "Main",
                MainIngredientName = "Chickpeas",
                FlavorTags = "spicy,savory,tangy",
                Instructions = "1. Soak chickpeas overnight and boil until tender.\n2. Fry onion until golden, add garlic and ginger paste.\n3. Add tomatoes and cook to a thick masala.\n4. Stir in cumin, coriander, garam masala and chickpeas.\n5. Simmer 20 minutes. Serve with rice or bhatura.",
                PrepTimeMinutes = 20,
                CookTimeMinutes = 40,
                Servings = 4,
                ImageUrl = ""
            },
            new Recipe
            {
                Id = 2,
                Name = "Chicken Schnitzel",
                Description = "Crispy golden Austrian-style breaded chicken breast.",
                Country = "Austria",
                Category = "Main",
                MainIngredientName = "Chicken Breast",
                FlavorTags = "savory,crispy",
                Instructions = "1. Pound chicken breast to even thickness.\n2. Season with salt and pepper.\n3. Coat in flour, dip in beaten egg, then breadcrumbs.\n4. Fry in hot oil 3-4 min per side until golden.\n5. Drain and serve with lemon.",
                PrepTimeMinutes = 15,
                CookTimeMinutes = 10,
                Servings = 2,
                ImageUrl = ""
            },
            new Recipe
            {
                Id = 3,
                Name = "Hash Browns",
                Description = "Classic American crispy shredded potato patties.",
                Country = "United States",
                Category = "Breakfast",
                MainIngredientName = "Potato",
                FlavorTags = "savory,crispy",
                Instructions = "1. Grate potatoes and squeeze out all moisture.\n2. Season with salt and pepper.\n3. Press into patties and fry in butter on medium-high.\n4. Cook 4-5 min per side until deep golden brown.",
                PrepTimeMinutes = 10,
                CookTimeMinutes = 15,
                Servings = 2,
                ImageUrl = ""
            },
            new Recipe
            {
                Id = 4,
                Name = "Pad Kra Pao",
                Description = "Thai stir-fried minced chicken with holy basil — Thailand's most beloved street food.",
                Country = "Thailand",
                Category = "Main",
                MainIngredientName = "Chicken Breast",
                FlavorTags = "spicy,savory,aromatic",
                Instructions = "1. Stir-fry garlic and chili in oil.\n2. Add minced chicken, cook through.\n3. Season with fish sauce, oyster sauce, soy sauce.\n4. Toss in holy basil off heat.\n5. Serve over jasmine rice with a fried egg.",
                PrepTimeMinutes = 10,
                CookTimeMinutes = 8,
                Servings = 2,
                ImageUrl = ""
            }
        );

        // RecipeIngredients
        b.Entity<RecipeIngredient>().HasData(
            // Channa Masala
            new RecipeIngredient { RecipeId = 1, IngredientId = 2,  Quantity = "400g",    SortOrder = 1 },
            new RecipeIngredient { RecipeId = 1, IngredientId = 9,  Quantity = "2 large", SortOrder = 2 },
            new RecipeIngredient { RecipeId = 1, IngredientId = 10, Quantity = "1 large", SortOrder = 3 },
            new RecipeIngredient { RecipeId = 1, IngredientId = 11, Quantity = "4 cloves",SortOrder = 4 },
            new RecipeIngredient { RecipeId = 1, IngredientId = 12, Quantity = "1 inch",  SortOrder = 5 },
            new RecipeIngredient { RecipeId = 1, IngredientId = 13, Quantity = "1 tsp",   SortOrder = 6 },
            new RecipeIngredient { RecipeId = 1, IngredientId = 14, Quantity = "2 tsp",   SortOrder = 7 },
            new RecipeIngredient { RecipeId = 1, IngredientId = 15, Quantity = "1 tsp",   SortOrder = 8 },
            // Schnitzel
            new RecipeIngredient { RecipeId = 2, IngredientId = 1,  Quantity = "2 pieces",SortOrder = 1 },
            new RecipeIngredient { RecipeId = 2, IngredientId = 7,  Quantity = "2",       SortOrder = 2 },
            new RecipeIngredient { RecipeId = 2, IngredientId = 8,  Quantity = "1 cup",   SortOrder = 3 },
            new RecipeIngredient { RecipeId = 2, IngredientId = 20, Quantity = "½ cup",   SortOrder = 4 },
            // Hash Browns
            new RecipeIngredient { RecipeId = 3, IngredientId = 3,  Quantity = "3 large", SortOrder = 1 },
            // Pad Kra Pao
            new RecipeIngredient { RecipeId = 4, IngredientId = 1,  Quantity = "300g minced", SortOrder = 1 },
            new RecipeIngredient { RecipeId = 4, IngredientId = 4,  Quantity = "1 cup",   SortOrder = 2 },
            new RecipeIngredient { RecipeId = 4, IngredientId = 11, Quantity = "5 cloves",SortOrder = 3 },
            new RecipeIngredient { RecipeId = 4, IngredientId = 19, Quantity = "3",       SortOrder = 4 },
            new RecipeIngredient { RecipeId = 4, IngredientId = 16, Quantity = "1 tbsp",  SortOrder = 5 },
            new RecipeIngredient { RecipeId = 4, IngredientId = 17, Quantity = "1 tbsp",  SortOrder = 6 },
            new RecipeIngredient { RecipeId = 4, IngredientId = 18, Quantity = "½ tbsp",  SortOrder = 7 }
        );
    }
}
