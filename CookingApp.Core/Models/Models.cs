namespace CookingApp.Core.Models;

// ─── Recipe ───────────────────────────────────────────────────────────────────

public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Browsing dimensions
    public string Country { get; set; } = string.Empty;          // e.g. "India"
    public string Category { get; set; } = string.Empty;         // Breakfast|Snack|Starter|Main|Dessert
    public string MainIngredientName { get; set; } = string.Empty; // e.g. "Chicken Breast"

    // Flavour profile (comma-separated tags for simplicity)
    public string FlavorTags { get; set; } = string.Empty;       // "spicy,savory"

    public string Instructions { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<UserSavedRecipe> SavedByUsers { get; set; } = new List<UserSavedRecipe>();
}

// ─── Ingredient ───────────────────────────────────────────────────────────────

public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;   // herb|protein|vegetable|spice|dairy|grain…
    public string? Notes { get; set; }

    // Navigation
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    // Substitutes where THIS ingredient is the original
    public ICollection<IngredientSubstitute> Substitutes { get; set; } = new List<IngredientSubstitute>();
}

// ─── RecipeIngredient (join table) ────────────────────────────────────────────

public class RecipeIngredient
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    public string Quantity { get; set; } = string.Empty;   // "2 cups", "1 tbsp"
    public bool IsOptional { get; set; }
    public int SortOrder { get; set; }
}

// ─── IngredientSubstitute ─────────────────────────────────────────────────────

public class IngredientSubstitute
{
    public int Id { get; set; }

    public int OriginalIngredientId { get; set; }
    public Ingredient OriginalIngredient { get; set; } = null!;

    public int SubstituteIngredientId { get; set; }
    public Ingredient SubstituteIngredient { get; set; } = null!;

    // 1 = identical, 2 = very close, 3 = acceptable, 4 = last resort
    public int ClosenessRank { get; set; }

    // e.g. "Thai basil works well but lacks the peppery bite of holy basil"
    public string Explanation { get; set; } = string.Empty;

    // How the dish changes: "Slightly sweeter, less peppery" or null if negligible
    public string? DishImpact { get; set; }
}

// ─── User ─────────────────────────────────────────────────────────────────────

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public UserPreferences? Preferences { get; set; }
    public ICollection<UserSavedRecipe> SavedRecipes { get; set; } = new List<UserSavedRecipe>();
}

// ─── UserPreferences ──────────────────────────────────────────────────────────

public class UserPreferences
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Comma-separated flavor tags the user dislikes: "spicy,bitter"
    public string DislikedFlavors { get; set; } = string.Empty;

    // Navigation to disliked ingredients
    public ICollection<UserDislikedIngredient> DislikedIngredients { get; set; } = new List<UserDislikedIngredient>();
}

// ─── UserDislikedIngredient (join table) ──────────────────────────────────────

public class UserDislikedIngredient
{
    public int UserPreferencesId { get; set; }
    public UserPreferences UserPreferences { get; set; } = null!;

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
}

// ─── UserSavedRecipe ──────────────────────────────────────────────────────────

public class UserSavedRecipe
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
