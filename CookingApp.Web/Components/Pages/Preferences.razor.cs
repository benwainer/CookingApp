using CookingApp.Core.DTOs;

namespace CookingApp.Web.Components.Pages;

public partial class Preferences
{
    private readonly string[] allFlavors = ["spicy", "sweet", "sour", "salty", "bitter", "savory", "crispy", "aromatic"];

    private List<string> dislikedFlavors = [];
    private List<IngredientOption> dislikedIngredients = [];
    private List<IngredientOption> ingredientSuggestions = [];
    private List<SavedRecipeSummaryDto> savedRecipes = [];
    private string ingredientSearch = string.Empty;
    private bool isSaving = false;
    private bool isLoading = true;
    private string? toastMessage;
    private System.Timers.Timer? toastTimer;

    protected override async Task OnInitializedAsync()
    {
        await Auth.InitializeAsync();

        if (!Auth.IsLoggedIn)
        {
            isLoading = false;
            return;
        }

        var prefsTask = Api.GetPreferencesAsync();
        var savedTask = Api.GetSavedRecipesAsync();
        await Task.WhenAll(prefsTask, savedTask);

        var prefs = await prefsTask;
        if (prefs != null)
        {
            dislikedFlavors = prefs.DislikedFlavors.ToList();
            // Preferences page uses canonical IDs for persistence but doesn't display disliked ingredients
            // (managed via the dislike buttons on RecipeDetail instead)
        }

        savedRecipes = await savedTask;
        isLoading = false;
    }

    private void ToggleFlavor(string flavor)
    {
        if (dislikedFlavors.Contains(flavor))
            dislikedFlavors.Remove(flavor);
        else
            dislikedFlavors.Add(flavor);
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
    }

    private void RemoveDislikedIngredient(int id) =>
        dislikedIngredients.RemoveAll(i => i.IngredientId == id);

    private async Task Save()
    {
        isSaving = true;
        await Api.SavePreferencesAsync(new UserPreferencesDto(
            dislikedFlavors,
            []   // canonical disliked ingredients are managed via RecipeDetail dislike buttons
        ));
        isSaving = false;
        ShowToast("✅ Preferences saved!");
    }

    private void ShowToast(string message)
    {
        toastTimer?.Dispose();
        toastMessage = message;
        StateHasChanged();

        toastTimer = new System.Timers.Timer(3000);
        toastTimer.Elapsed += async (_, _) =>
        {
            toastTimer?.Dispose();
            toastMessage = null;
            await InvokeAsync(StateHasChanged);
        };
        toastTimer.AutoReset = false;
        toastTimer.Start();
    }

    private record IngredientOption(int IngredientId, string IngredientName);
}
