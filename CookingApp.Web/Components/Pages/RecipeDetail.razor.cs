using System.Text.RegularExpressions;
using CookingApp.Core.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CookingApp.Web.Components.Pages;

public partial class RecipeDetail
{
    [Parameter] public int Id { get; set; }
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private RecipeDetailDto? recipe;
    private bool isLoading = true;
    private bool isSaved = false;
    private string backUrl = "/browse";

    // ingredientId → SubstituteState
    private Dictionary<int, SubstituteState> substituteState = new();

    // ingredientName → list of nearby stores
    private Dictionary<string, List<NearbyStoreDto>> storeResults = new();

    // Track which substitute ranks have been shown per ingredient
    private Dictionary<int, List<int>> suggestedRanks = new();

    // Ingredients that have at least one substitute available
    private HashSet<int> ingredientsWithSubstitutes = [];

    // Ingredient checklist
    private HashSet<int> checkedIngredients = [];

    // Disliked canonical ingredient IDs for this user
    private HashSet<int> dislikedCanonicalIds = [];

    // Unit conversion
    private enum UnitSystem { Metric, Imperial, Cups, Tbsp }
    private UnitSystem selectedUnit = UnitSystem.Metric;

    // Servings scaler
    private int servingsMultiplier = 1;

    // Step-by-step mode
    private bool stepByStepMode = false;
    private int currentStepIndex = 0;
    private string[] instructionSteps = [];

    // Toast notification
    private string? toastMessage;
    private System.Timers.Timer? toastTimer;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(ReturnUrl))
            backUrl = Uri.UnescapeDataString(ReturnUrl);

        recipe = await Api.GetRecipeAsync(Id);
        isLoading = false;

        if (recipe != null)
        {
            instructionSteps = recipe.Instructions
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.TrimStart('0','1','2','3','4','5','6','7','8','9','.',' '))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            if (Auth.IsLoggedIn)
            {
                isSaved = await Api.IsRecipeSavedAsync(Id);
                var disliked = await Api.GetDislikedIngredientsAsync();
                dislikedCanonicalIds = [..disliked.Select(d => d.CanonicalIngredientId)];

                // Auto-show substitutes for disliked ingredients in this recipe
                if (recipe != null)
                {
                    var dislikedInRecipe = recipe.Ingredients
                        .Where(i => i.CanonicalIngredientId.HasValue
                            && dislikedCanonicalIds.Contains(i.CanonicalIngredientId.Value))
                        .ToList();
                    foreach (var ing in dislikedInRecipe)
                        _ = RequestSubstitute(ing.IngredientId, ing.IngredientName);
                }
            }
        }

        StateHasChanged();

        _ = CheckSubstituteAvailabilityAsync();
    }

    private async Task CheckSubstituteAvailabilityAsync()
    {
        if (recipe == null) return;
        try
        {
            var checks = recipe.Ingredients.Select(async ing =>
            {
                var sub = await Api.GetNextSubstituteAsync(ing.IngredientId, []);
                if (sub != null)
                    ingredientsWithSubstitutes.Add(ing.IngredientId);
            });
            await Task.WhenAll(checks);
        }
        catch
        {
            // Substitute check failed — buttons simply won't show
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RequestSubstitute(int ingredientId, string ingredientName)
    {
        try
        {
            if (!substituteState.ContainsKey(ingredientId))
                substituteState[ingredientId] = new SubstituteState();
            if (!suggestedRanks.ContainsKey(ingredientId))
                suggestedRanks[ingredientId] = [];

            substituteState[ingredientId].IsLoading = true;
            StateHasChanged();

            var sub = await Api.GetNextSubstituteAsync(ingredientId, suggestedRanks[ingredientId]);

            if (sub != null)
            {
                suggestedRanks[ingredientId].Add(sub.ClosenessRank);
                substituteState[ingredientId] = new SubstituteState { Sub = sub, IsLoading = false };
            }
            else
            {
                substituteState[ingredientId] = new SubstituteState { Sub = null, IsLoading = false };
                ingredientsWithSubstitutes.Remove(ingredientId);
            }
        }
        catch
        {
            substituteState[ingredientId] = new SubstituteState { Sub = null, IsLoading = false };
            ingredientsWithSubstitutes.Remove(ingredientId);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task FindStore(string ingredientName)
    {
        var (lat, lng) = await Location.GetCoordinatesAsync();
        var stores = await Api.GetNearbyStoresAsync(ingredientName, lat, lng);
        storeResults[ingredientName] = stores;
        StateHasChanged();
    }

    private async Task ToggleSave()
    {
        if (isSaved)
        {
            await Api.UnsaveRecipeAsync(Id);
            isSaved = false;
            ShowToast("Removed from saved recipes");
        }
        else
        {
            await Api.SaveRecipeAsync(Id);
            isSaved = true;
            ShowToast("Recipe saved! ❤️");
        }
    }

    private async Task ToggleDislike(int canonicalIngredientId)
    {
        if (dislikedCanonicalIds.Contains(canonicalIngredientId))
        {
            await Api.UnDislikeIngredientAsync(canonicalIngredientId);
            dislikedCanonicalIds.Remove(canonicalIngredientId);
        }
        else
        {
            await Api.DislikeIngredientAsync(canonicalIngredientId);
            dislikedCanonicalIds.Add(canonicalIngredientId);
        }
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

    private async Task ShareRecipe()
    {
        var url = Nav.Uri;
        await JS.InvokeVoidAsync("copyToClipboard", url);
        ShowToast("Link copied to clipboard!");
    }

    // Ingredient checklist
    private void ToggleIngredientCheck(int ingredientId)
    {
        if (!checkedIngredients.Add(ingredientId))
            checkedIngredients.Remove(ingredientId);
    }

    // Servings scaler
    private void IncreaseServings() { if (servingsMultiplier < 10) servingsMultiplier++; }
    private void DecreaseServings() { if (servingsMultiplier > 1) servingsMultiplier--; }

    // Step-by-step navigation
    private void ToggleStepMode()
    {
        stepByStepMode = !stepByStepMode;
        currentStepIndex = 0;
    }

    private int StepProgressPct => instructionSteps.Length > 0
        ? (int)Math.Round((currentStepIndex + 1) * 100.0 / instructionSteps.Length)
        : 0;

    private void NextStep() { if (currentStepIndex < instructionSteps.Length - 1) currentStepIndex++; }
    private void PrevStep() { if (currentStepIndex > 0) currentStepIndex--; }

    [Inject] private NavigationManager Nav { get; set; } = default!;

    private static readonly string[] Descriptors =
    [
        "finely chopped", "roughly chopped", "chopped", "thinly sliced", "thickly sliced",
        "sliced", "diced", "peeled and crushed", "peeled crushed", "crushed", "minced",
        "grated", "cut into strips", "cut into chunks", "cut into pieces", "to taste",
        "peeled", "deseeded", "trimmed", "cooked", "melted", "softened", "beaten",
        "sifted", "rinsed", "shredded", "torn", "julienned", "cubed", "halved",
        "quartered", "whole", "fresh", "dried", "frozen", "canned", "packed",
        "heaped", "heaping", "level", "rounded", "scant"
    ];

    private static (string numericPart, string descriptor) SplitQuantity(string raw)
    {
        string lower = raw.ToLowerInvariant();
        foreach (var desc in Descriptors)
        {
            int idx = lower.IndexOf(desc, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;

            bool startOk = idx == 0 || !char.IsLetter(raw[idx - 1]);
            bool endOk   = idx + desc.Length >= raw.Length || !char.IsLetter(raw[idx + desc.Length]);
            if (!startOk || !endOk) continue;

            if (idx > 0)
            {
                string numericPart = raw[..idx].Trim().TrimEnd(',');
                return (numericPart, desc);
            }

            string remainder = raw[(idx + desc.Length)..].Trim().TrimStart(',');
            return (remainder, desc);
        }
        return (raw, string.Empty);
    }

    private static readonly Regex UnitRegex =
        new(@"(\d+\.?\d*)\s*(g|kg|ml|l|tbsp|tsp|cup|cups|fl\s*oz|oz|lbs|lb)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private string ConvertQuantity(string raw, string ingredientName)
    {
        var (numericPart, _) = SplitQuantity(raw);

        var match = UnitRegex.Match(numericPart);
        if (!match.Success)
        {
            // Try to scale plain numbers
            if (servingsMultiplier > 1 && double.TryParse(numericPart.Trim(), out var plain))
                return $"{plain * servingsMultiplier:0.##}";
            return numericPart;
        }

        double value = double.Parse(match.Groups[1].Value) * servingsMultiplier;
        string unit  = match.Groups[2].Value.ToLowerInvariant().Replace(" ", "");

        string converted = (selectedUnit, unit) switch
        {
            (UnitSystem.Metric, "cup")   => $"{value * 240:0.##} ml",
            (UnitSystem.Metric, "cups")  => $"{value * 240:0.##} ml",
            (UnitSystem.Metric, "tbsp")  => $"{value * 15:0.##} ml",
            (UnitSystem.Metric, "tsp")   => $"{value * 5:0.##} ml",
            (UnitSystem.Metric, "floz")  => $"{value * 29.57:0.##} ml",
            (UnitSystem.Metric, "lb")    => $"{value * 0.453592:0.##} kg",
            (UnitSystem.Metric, "lbs")   => $"{value * 0.453592:0.##} kg",
            (UnitSystem.Metric, "oz")    => $"{value * 28.35:0.##} g",
            (UnitSystem.Imperial, "g")   => $"{value / 28.35:0.##} oz",
            (UnitSystem.Imperial, "kg")  => $"{value * 2.205:0.##} lbs",
            (UnitSystem.Imperial, "ml")  => $"{value / 29.57:0.##} fl oz",
            (UnitSystem.Imperial, "l")   => $"{value * 33.814:0.##} fl oz",
            (UnitSystem.Cups, "ml")      => $"{value / 240:0.##} cups",
            (UnitSystem.Cups, "l")       => $"{value * 1000 / 240:0.##} cups",
            (UnitSystem.Cups, "g")       => GramsToCups(value, ingredientName),
            (UnitSystem.Cups, "kg")      => GramsToCups(value * 1000, ingredientName),
            (UnitSystem.Cups, "tbsp")    => $"{value / 16:0.##} cups",
            (UnitSystem.Cups, "tsp")     => $"{value / 48:0.##} cups",
            (UnitSystem.Tbsp, "ml")      => $"{value / 15:0.##} tbsp",
            (UnitSystem.Tbsp, "l")       => $"{value * 1000 / 15:0.##} tbsp",
            (UnitSystem.Tbsp, "g")       => $"{value / 9:0.##} tbsp",
            (UnitSystem.Tbsp, "kg")      => $"{value * 1000 / 9:0.##} tbsp",
            (UnitSystem.Tbsp, "tsp")     => $"{value / 3:0.##} tbsp",
            (UnitSystem.Tbsp, "cup")     => $"{value * 16:0.##} tbsp",
            (UnitSystem.Tbsp, "cups")    => $"{value * 16:0.##} tbsp",
            _                            => $"{value:0.##} {unit}"
        };

        string tail = numericPart[(match.Index + match.Length)..].Trim();
        return tail.Length > 0 ? $"{converted} {tail}" : converted;
    }

    private static string GramsToCups(double grams, string ingredientName)
    {
        double gramsPerCup = ingredientName.ToLowerInvariant() switch
        {
            var n when n.Contains("flour")  => 120,
            var n when n.Contains("sugar")  => 200,
            var n when n.Contains("butter") => 227,
            var n when n.Contains("rice")   => 185,
            _                               => 0
        };

        return gramsPerCup > 0
            ? $"{grams / gramsPerCup:0.##} cups"
            : $"{grams / 28.35:0.##} oz";
    }

    private static string ClosenessLabel(int rank) => rank switch
    {
        1 => "Identical",
        2 => "Very close",
        3 => "Acceptable",
        4 => "Last resort",
        _ => "Substitute"
    };

    private class SubstituteState
    {
        public SubstituteDto? Sub { get; set; }
        public bool IsLoading { get; set; }
    }
}
