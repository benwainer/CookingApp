using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using CookingApp.Core.DTOs;

namespace CookingApp.Web.Services;

/// <summary>
/// Typed HTTP client used by all Blazor pages to talk to CookingApp.API.
/// Automatically attaches the JWT token from local storage on every request.
/// </summary>
public class ApiClient(HttpClient http, ILocalStorageService storage)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Auth ──────────────────────────────────────────────────────────────────

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var res = await http.PostAsJsonAsync("/api/auth/login", req);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts))!;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        var res = await http.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts))!;
    }

    // ── Recipes ───────────────────────────────────────────────────────────────

    public async Task<List<RecipeSummaryDto>> SearchRecipesAsync(RecipeSearchRequest req)
    {
        await AttachTokenAsync();
        var qs = BuildRecipeQueryString(req);
        return await http.GetFromJsonAsync<List<RecipeSummaryDto>>($"/api/recipes?{qs}", JsonOpts)
               ?? [];
    }

    public async Task<RecipeDetailDto?> GetRecipeAsync(int id)
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<RecipeDetailDto>($"/api/recipes/{id}", JsonOpts);
    }

    public async Task<List<AutocompleteResult>> GetAutocompleteAsync(string prefix)
    {
        return await http.GetFromJsonAsync<List<AutocompleteResult>>(
            $"/api/recipes/autocomplete?prefix={Uri.EscapeDataString(prefix)}", JsonOpts)
               ?? [];
    }

    public Task<List<string>> GetCountriesAsync() =>
        http.GetFromJsonAsync<List<string>>("/api/recipes/countries", JsonOpts)
            .ContinueWith(t => t.Result ?? []);

    public Task<List<string>> GetIngredientsAsync() =>
        http.GetFromJsonAsync<List<string>>("/api/recipes/main-ingredients", JsonOpts)
            .ContinueWith(t => t.Result ?? []);

    public async Task<Dictionary<string, List<string>>> GetIngredientsByCategoryAsync()
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<Dictionary<string, List<string>>>(
            "/api/recipes/canonical-ingredients", JsonOpts) ?? new();
    }

    // ── Substitutes ───────────────────────────────────────────────────────────

    public async Task<SubstituteDto?> GetNextSubstituteAsync(int ingredientId, List<int> alreadySuggested)
    {
        var suggested = string.Join(',', alreadySuggested);
        return await http.GetFromJsonAsync<SubstituteDto?>(
            $"/api/substitutes/{ingredientId}?suggested={suggested}", JsonOpts);
    }

    // ── User ──────────────────────────────────────────────────────────────────

    public async Task<UserPreferencesDto?> GetPreferencesAsync()
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<UserPreferencesDto>("/api/users/preferences", JsonOpts);
    }

    public async Task SavePreferencesAsync(UserPreferencesDto dto)
    {
        await AttachTokenAsync();
        var res = await http.PutAsJsonAsync("/api/users/preferences", dto);
        res.EnsureSuccessStatusCode();
    }

    public async Task<List<SavedRecipeSummaryDto>> GetSavedRecipesAsync()
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<List<SavedRecipeSummaryDto>>("/api/users/saved-recipes", JsonOpts) ?? [];
    }

    public async Task<bool> IsRecipeSavedAsync(int id)
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<bool>($"/api/users/saved-recipes/{id}", JsonOpts);
    }

    public async Task SaveRecipeAsync(int id)
    {
        await AttachTokenAsync();
        await http.PostAsync($"/api/users/saved-recipes/{id}", null);
    }

    public async Task UnsaveRecipeAsync(int id)
    {
        await AttachTokenAsync();
        await http.DeleteAsync($"/api/users/saved-recipes/{id}");
    }

    public async Task UpdateRecipeNotesAsync(int id, string? notes)
    {
        await AttachTokenAsync();
        await http.PatchAsJsonAsync($"/api/users/saved-recipes/{id}/notes",
            new UpdateRecipeNotesRequest(notes));
    }

    public async Task<List<DislikedIngredientDto>> GetDislikedIngredientsAsync()
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<List<DislikedIngredientDto>>(
            "/api/users/disliked-ingredients", JsonOpts) ?? [];
    }

    public async Task DislikeIngredientAsync(int canonicalIngredientId)
    {
        await AttachTokenAsync();
        await http.PostAsync($"/api/users/dislike-ingredient/{canonicalIngredientId}", null);
    }

    public async Task UnDislikeIngredientAsync(int canonicalIngredientId)
    {
        await AttachTokenAsync();
        await http.DeleteAsync($"/api/users/dislike-ingredient/{canonicalIngredientId}");
    }

    // ── AI ────────────────────────────────────────────────────────────────────

    public async Task<AiChatResponse> AiChatAsync(AiChatRequest req)
    {
        await AttachTokenAsync();
        var res = await http.PostAsJsonAsync("/api/ai/chat", req);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AiChatResponse>(JsonOpts))!;
    }

    // ── Stores ────────────────────────────────────────────────────────────────

    public async Task<List<NearbyStoreDto>> GetNearbyStoresAsync(string ingredient, double lat, double lng)
    {
        return await http.GetFromJsonAsync<List<NearbyStoreDto>>(
            $"/api/stores/nearby?ingredient={Uri.EscapeDataString(ingredient)}&lat={lat}&lng={lng}",
            JsonOpts) ?? [];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task AttachTokenAsync()
    {
        var token = await storage.GetItemAsync<string>("auth_token");
        if (!string.IsNullOrEmpty(token))
        {
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static string BuildRecipeQueryString(RecipeSearchRequest req)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(req.Query))       parts.Add($"query={Uri.EscapeDataString(req.Query)}");
        if (!string.IsNullOrEmpty(req.Country))     parts.Add($"country={Uri.EscapeDataString(req.Country)}");
        if (!string.IsNullOrEmpty(req.MainIngredient)) parts.Add($"mainIngredient={Uri.EscapeDataString(req.MainIngredient)}");
        if (!string.IsNullOrEmpty(req.Category))    parts.Add($"category={Uri.EscapeDataString(req.Category)}");
        if (req.Flavors?.Count > 0)
            foreach (var f in req.Flavors)
                parts.Add($"flavors={Uri.EscapeDataString(f)}");
        parts.Add($"page={req.Page}");
        parts.Add($"pageSize={req.PageSize}");
        return string.Join('&', parts);
    }
}
