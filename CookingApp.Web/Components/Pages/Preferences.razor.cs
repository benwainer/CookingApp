using CookingApp.Core.DTOs;

namespace CookingApp.Web.Components.Pages;

public partial class Preferences
{
    private readonly string[] allFlavors = ["spicy", "sweet", "sour", "salty", "bitter", "savory", "crispy", "aromatic"];

    private List<string> dislikedFlavors = [];
    private List<IngredientOption> dislikedIngredients = [];
    private List<IngredientOption> ingredientSuggestions = [];
    private string ingredientSearch = string.Empty;
    private bool isSaving = false;
    private bool isSaved = false;

    protected override async Task OnInitializedAsync()
    {
        if (!Auth.IsLoggedIn) return;

        var prefs = await Api.GetPreferencesAsync();
        if (prefs != null)
        {
            dislikedFlavors = prefs.DislikedFlavors.ToList();
            // Load ingredient names for the saved IDs
            // For now we store them as-is; a full app would resolve names
        }
    }

    private void ToggleFlavor(string flavor)
    {
        if (dislikedFlavors.Contains(flavor))
            dislikedFlavors.Remove(flavor);
        else
            dislikedFlavors.Add(flavor);
        isSaved = false;
    }

    private async Task OnIngredientInput(Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        ingredientSearch = e.Value?.ToString() ?? string.Empty;
        if (ingredientSearch.Length >= 2)
        {
            var results = await Api.GetAutocompleteAsync(ingredientSearch);
            ingredientSuggestions = results
                .Where(r => r.Type == "ingredient")
                .Select(r => new IngredientOption(0, r.Value))
                .ToList();
        }
        else
        {
            ingredientSuggestions = [];
        }
    }

    private void AddDislikedIngredient(IngredientOption ing)
    {
        if (!dislikedIngredients.Any(d => d.IngredientName == ing.IngredientName))
            dislikedIngredients.Add(ing);
        ingredientSearch = string.Empty;
        ingredientSuggestions = [];
        isSaved = false;
    }

    private void RemoveDislikedIngredient(int id) =>
        dislikedIngredients.RemoveAll(i => i.IngredientId == id);

    private async Task Save()
    {
        isSaving = true;
        await Api.SavePreferencesAsync(new UserPreferencesDto(
            dislikedFlavors,
            dislikedIngredients.Select(i => i.IngredientId).ToList()
        ));
        isSaving = false;
        isSaved = true;
    }

    private record IngredientOption(int IngredientId, string IngredientName);
}
