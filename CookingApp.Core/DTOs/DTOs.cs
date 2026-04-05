namespace CookingApp.Core.DTOs;

// ─── Recipe DTOs ──────────────────────────────────────────────────────────────

/// <summary>Lightweight card shown in search results / browse grids.</summary>
public record RecipeSummaryDto(
    int Id,
    string Name,
    string Country,
    string Category,
    string MainIngredientName,
    string FlavorTags,
    int TotalTimeMinutes,
    string ImageUrl
);

/// <summary>Full recipe with all ingredients and steps.</summary>
public record RecipeDetailDto(
    int Id,
    string Name,
    string Description,
    string Country,
    string Category,
    string MainIngredientName,
    string FlavorTags,
    string Instructions,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string ImageUrl,
    List<RecipeIngredientDto> Ingredients
);

public record RecipeIngredientDto(
    int IngredientId,
    string IngredientName,
    string Quantity,
    bool IsOptional,
    int SortOrder
);

// ─── Search / Filter DTOs ─────────────────────────────────────────────────────

public record RecipeSearchRequest(
    string? Query,               // free text
    string? Country,
    string? MainIngredient,
    string? Category,            // Breakfast|Snack|Starter|Main|Dessert
    List<string>? Flavors,       // ["spicy","sweet"]
    int Page = 1,
    int PageSize = 20
);

public record AutocompleteResult(string Value, string Type); // Type = "country" | "ingredient"

// ─── Substitute DTOs ──────────────────────────────────────────────────────────

public record SubstituteDto(
    int SubstituteIngredientId,
    string SubstituteIngredientName,
    int ClosenessRank,
    string Explanation,
    string? DishImpact
);

// ─── User / Auth DTOs ─────────────────────────────────────────────────────────

public record RegisterRequest(string Email, string DisplayName, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, int UserId, string DisplayName);

// ─── User Preferences DTOs ───────────────────────────────────────────────────

public record UserPreferencesDto(
    List<string> DislikedFlavors,
    List<int> DislikedIngredientIds
);

// ─── AI Assistant DTOs ───────────────────────────────────────────────────────

public record AiChatMessage(string Role, string Content); // role = "user" | "assistant"

public record AiChatRequest(
    List<AiChatMessage> History,
    string NewMessage,
    int? CurrentRecipeId   // inject recipe context if user is viewing one
);

public record AiChatResponse(string Reply);

// ─── Store / Places DTOs ─────────────────────────────────────────────────────

public record NearbyStoreDto(
    string Name,
    string Address,
    double DistanceKm,
    bool IsOpenNow,
    string? GoogleMapsUrl
);
