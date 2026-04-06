using CookingApp.Core.DTOs;
using Microsoft.AspNetCore.Components;

namespace CookingApp.Web.Components.Pages;

public partial class Browse
{
    [SupplyParameterFromQuery] public string? Country { get; set; }
    [SupplyParameterFromQuery] public string? Ingredient { get; set; }
    [SupplyParameterFromQuery] public string? Category { get; set; }
    [SupplyParameterFromQuery] public string? Flavors { get; set; }

    private List<RecipeSummaryDto> recipes = [];
    private List<string> countries = [];
    private List<string> ingredients = [];
    private bool isLoading = true;

    private readonly string[] categories = ["Breakfast", "Snack", "Starter", "Main", "Dessert"];

    private string filterCategory = string.Empty;
    private string filterCountry = string.Empty;
    private string filterIngredient = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        // Pre-fill filters from query params
        filterCountry = Country ?? string.Empty;
        filterIngredient = Ingredient ?? string.Empty;
        filterCategory = Category ?? string.Empty;

        // Load dropdown options
        var countryTask = Api.GetCountriesAsync();
        var ingredientTask = Api.GetIngredientsAsync();
        await Task.WhenAll(countryTask, ingredientTask);
        countries = await countryTask;
        ingredients = await ingredientTask;

        await LoadRecipes();
    }

    private async Task LoadRecipes()
    {
        isLoading = true;
        StateHasChanged();

        var flavorList = Flavors?.Split(',').ToList();

        recipes = await Api.SearchRecipesAsync(new RecipeSearchRequest(
            Query: null,
            Country: string.IsNullOrEmpty(filterCountry) ? null : filterCountry,
            MainIngredient: string.IsNullOrEmpty(filterIngredient) ? null : filterIngredient,
            Category: string.IsNullOrEmpty(filterCategory) ? null : filterCategory,
            Flavors: flavorList
        ));

        isLoading = false;
    }

    private async Task ClearFilters()
    {
        filterCategory = string.Empty;
        filterCountry = string.Empty;
        filterIngredient = string.Empty;
        Flavors = null;
        await LoadRecipes();
    }

    private void OpenRecipe(int id) => Nav.NavigateTo($"/recipe/{id}");
}
