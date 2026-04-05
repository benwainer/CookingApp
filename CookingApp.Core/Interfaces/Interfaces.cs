using CookingApp.Core.DTOs;
using CookingApp.Core.Models;

namespace CookingApp.Core.Interfaces;

// ─── Recipe ───────────────────────────────────────────────────────────────────

public interface IRecipeRepository
{
    Task<RecipeDetailDto?> GetByIdAsync(int id);
    Task<List<RecipeSummaryDto>> SearchAsync(RecipeSearchRequest request, UserPreferencesDto? prefs);
    Task<List<AutocompleteResult>> AutocompleteAsync(string prefix);
    Task<List<string>> GetAllCountriesAsync();
    Task<List<string>> GetAllMainIngredientsAsync();
}

// ─── Substitute ───────────────────────────────────────────────────────────────

public interface ISubstituteRepository
{
    /// <summary>Returns substitutes for an ingredient, ordered by ClosenessRank.</summary>
    Task<List<SubstituteDto>> GetSubstitutesAsync(int ingredientId);
}

// ─── User ─────────────────────────────────────────────────────────────────────

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(User user);
    Task<UserPreferences?> GetPreferencesAsync(int userId);
    Task UpsertPreferencesAsync(int userId, UserPreferencesDto dto);
    Task SaveRecipeAsync(int userId, int recipeId);
    Task UnsaveRecipeAsync(int userId, int recipeId);
    Task<List<RecipeSummaryDto>> GetSavedRecipesAsync(int userId);
}

// ─── Services ─────────────────────────────────────────────────────────────────

public interface IRecipeService
{
    Task<List<RecipeSummaryDto>> SearchAsync(RecipeSearchRequest request, int? userId);
    Task<RecipeDetailDto?> GetDetailAsync(int id);
    Task<List<AutocompleteResult>> AutocompleteAsync(string prefix);
}

public interface ISubstituteService
{
    /// <summary>
    /// Returns the next unused substitute for the ingredient in this session.
    /// Tracks which ranks have already been suggested via <paramref name="alreadySuggestedRanks"/>.
    /// Returns null when all substitutes are exhausted.
    /// </summary>
    Task<SubstituteDto?> GetNextSubstituteAsync(int ingredientId, List<int> alreadySuggestedRanks);
}

public interface IUserService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserPreferencesDto?> GetPreferencesAsync(int userId);
    Task SavePreferencesAsync(int userId, UserPreferencesDto dto);
    Task SaveRecipeAsync(int userId, int recipeId);
    Task UnsaveRecipeAsync(int userId, int recipeId);
    Task<List<RecipeSummaryDto>> GetSavedRecipesAsync(int userId);
}

public interface IAiAssistantService
{
    Task<AiChatResponse> ChatAsync(AiChatRequest request);
}

public interface IPlacesService
{
    Task<List<NearbyStoreDto>> FindNearbyStoresAsync(string ingredientName, double lat, double lng);
}
