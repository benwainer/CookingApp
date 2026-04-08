using CookingApp.Core.DTOs;
using CookingApp.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CookingApp.Web.Components.Pages;

public partial class Browse
{
    [Inject] private AuthStateService Auth { get; set; } = default!;
    [SupplyParameterFromQuery] public string? Country { get; set; }
    [SupplyParameterFromQuery] public string? Ingredient { get; set; }
    [SupplyParameterFromQuery] public string? Category { get; set; }
    [SupplyParameterFromQuery] public string? Flavors { get; set; }

    private List<RecipeSummaryDto> recipes = [];
    private List<string> countries = [];
    private List<string> ingredients = [];
    private Dictionary<string, List<string>> ingredientsByCategory = new();
    private bool isLoading = true;
    private bool isLoadingMore = false;
    private bool hasMore = false;
    private int currentPage = 1;
    private const int PageSize = 20;

    private readonly string[] categories = ["Breakfast", "Snack", "Starter", "Main", "Dessert"];

    private string filterCategory = string.Empty;
    private string filterCountry = string.Empty;
    private string filterIngredient = string.Empty;
    private string sortBy = "default";
    private bool _isUpdatingUrl = false;

    private List<DislikedIngredientDto> dislikedIngredients = [];

    private IEnumerable<RecipeSummaryDto> SortedRecipes => sortBy switch
    {
        "quickest"  => recipes.OrderBy(r => r.TotalTimeMinutes),
        "name-asc"  => recipes.OrderBy(r => r.Name),
        "name-desc" => recipes.OrderByDescending(r => r.Name),
        _           => recipes
    };

    protected override async Task OnInitializedAsync()
    {
        filterCountry    = Country    ?? string.Empty;
        filterIngredient = Ingredient ?? string.Empty;
        filterCategory   = Category   ?? string.Empty;

        await Auth.InitializeAsync();

        var countryTask    = Api.GetCountriesAsync();
        var ingredientTask = Api.GetIngredientsByCategoryAsync();
        var dislikedTask   = Auth.IsLoggedIn ? Api.GetDislikedIngredientsAsync() : Task.FromResult(new List<DislikedIngredientDto>());

        await Task.WhenAll(countryTask, ingredientTask, dislikedTask);
        countries             = await countryTask;
        ingredientsByCategory = await ingredientTask;
        dislikedIngredients   = await dislikedTask;

        await LoadRecipes();
    }

    private async Task LoadRecipes()
    {
        isLoading = true;
        currentPage = 1;
        StateHasChanged();

        var flavorList = Flavors?.Split(',').ToList();

        var page = await Api.SearchRecipesAsync(new RecipeSearchRequest(
            Query:          null,
            Country:        string.IsNullOrEmpty(filterCountry)    ? null : filterCountry,
            MainIngredient: string.IsNullOrEmpty(filterIngredient) ? null : filterIngredient,
            Category:       string.IsNullOrEmpty(filterCategory)   ? null : filterCategory,
            Flavors:        flavorList,
            Page:           1,
            PageSize:       PageSize
        ));

        recipes = page;
        hasMore = page.Count == PageSize;
        isLoading = false;
        await UpdateUrl();
    }

    private async Task UpdateUrl()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(filterCategory))
            parts.Add($"category={Uri.EscapeDataString(filterCategory)}");
        if (!string.IsNullOrEmpty(filterCountry))
            parts.Add($"country={Uri.EscapeDataString(filterCountry)}");
        if (!string.IsNullOrEmpty(filterIngredient))
            parts.Add($"ingredient={Uri.EscapeDataString(filterIngredient)}");

        var url = parts.Count > 0 ? $"/browse?{string.Join("&", parts)}" : "/browse";
        _isUpdatingUrl = true;
        await JS.InvokeVoidAsync("history.replaceState", null, "", url);
        _isUpdatingUrl = false;
    }

    private async Task LoadMore()
    {
        isLoadingMore = true;
        currentPage++;
        StateHasChanged();

        var flavorList = Flavors?.Split(',').ToList();

        var page = await Api.SearchRecipesAsync(new RecipeSearchRequest(
            Query:          null,
            Country:        string.IsNullOrEmpty(filterCountry)    ? null : filterCountry,
            MainIngredient: string.IsNullOrEmpty(filterIngredient) ? null : filterIngredient,
            Category:       string.IsNullOrEmpty(filterCategory)   ? null : filterCategory,
            Flavors:        flavorList,
            Page:           currentPage,
            PageSize:       PageSize
        ));

        recipes.AddRange(page);
        hasMore = page.Count == PageSize;
        isLoadingMore = false;
    }

    private async Task ClearFilters()
    {
        filterCategory   = string.Empty;
        filterCountry    = string.Empty;
        filterIngredient = string.Empty;
        Flavors          = null;
        sortBy           = "default";
        await LoadRecipes();
    }

    private async Task ClearFilter(string filter)
    {
        switch (filter)
        {
            case "category":   filterCategory   = string.Empty; break;
            case "country":    filterCountry    = string.Empty; break;
            case "ingredient": filterIngredient = string.Empty; break;
        }
        await LoadRecipes();
    }

    private async Task RemoveDislike(int canonicalIngredientId)
    {
        await Api.UnDislikeIngredientAsync(canonicalIngredientId);
        dislikedIngredients.RemoveAll(d => d.CanonicalIngredientId == canonicalIngredientId);
        await LoadRecipes();
    }

    private void OpenRecipe(int id) =>
        Nav.NavigateTo($"/recipe/{id}?returnUrl={Uri.EscapeDataString(Nav.Uri)}");
}
