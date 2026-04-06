using System.Text.RegularExpressions;
using CookingApp.Core.DTOs;
using Microsoft.AspNetCore.Components;

namespace CookingApp.Web.Components.Pages;

public partial class RecipeDetail
{
    [Parameter] public int Id { get; set; }

    private RecipeDetailDto? recipe;
    private bool isLoading = true;
    private bool isSaved = false;

    // ingredientId → SubstituteState
    private Dictionary<int, SubstituteState> substituteState = new();

    // ingredientName → list of nearby stores
    private Dictionary<string, List<NearbyStoreDto>> storeResults = new();

    // Track which substitute ranks have been shown per ingredient
    private Dictionary<int, List<int>> suggestedRanks = new();

    // Ingredients that have at least one substitute available
    private HashSet<int> ingredientsWithSubstitutes = [];

    // Unit conversion
    private enum UnitSystem { Metric, Imperial, Cups, Tbsp }
    private UnitSystem selectedUnit = UnitSystem.Metric;

    protected override async Task OnInitializedAsync()
    {
        recipe = await Api.GetRecipeAsync(Id);
        isLoading = false;
        StateHasChanged();

        // Fire substitute availability check in the background so the page
        // renders immediately — buttons appear once the check completes.
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
        }
        else
        {
            await Api.SaveRecipeAsync(Id);
            isSaved = true;
        }
    }

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

    // Returns (numericPart, descriptor) where numericPart has descriptors stripped.
    // Handles descriptors both before and after the numeric part.
    private static (string numericPart, string descriptor) SplitQuantity(string raw)
    {
        string lower = raw.ToLowerInvariant();
        foreach (var desc in Descriptors)
        {
            int idx = lower.IndexOf(desc, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;

            // Ensure it's not mid-word
            bool startOk = idx == 0 || !char.IsLetter(raw[idx - 1]);
            bool endOk   = idx + desc.Length >= raw.Length || !char.IsLetter(raw[idx + desc.Length]);
            if (!startOk || !endOk) continue;

            // Descriptor is after the numeric part
            if (idx > 0)
            {
                string numericPart = raw[..idx].Trim().TrimEnd(',');
                return (numericPart, desc);
            }

            // Descriptor is at the start — numeric part is whatever follows
            string remainder = raw[(idx + desc.Length)..].Trim().TrimStart(',');
            return (remainder, desc);
        }
        return (raw, string.Empty);
    }

    // Regex: optional leading number+unit block, e.g. "200g", "1.5 kg", "300 ml"
    private static readonly Regex UnitRegex =
        new(@"(\d+\.?\d*)\s*(g|kg|ml|l|tbsp|tsp|cup|cups|fl\s*oz|oz|lbs|lb)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private string ConvertQuantity(string raw, string ingredientName)
    {
        var (numericPart, _) = SplitQuantity(raw);

        var match = UnitRegex.Match(numericPart);
        if (!match.Success) return numericPart;

        double value = double.Parse(match.Groups[1].Value);
        string unit  = match.Groups[2].Value.ToLowerInvariant().Replace(" ", "");

        string converted = (selectedUnit, unit) switch
        {
            // ── Metric (convert non-metric units → metric) ────────────────────
            (UnitSystem.Metric, "cup")   => $"{value * 240:0.##} ml",
            (UnitSystem.Metric, "cups")  => $"{value * 240:0.##} ml",
            (UnitSystem.Metric, "tbsp")  => $"{value * 15:0.##} ml",
            (UnitSystem.Metric, "tsp")   => $"{value * 5:0.##} ml",
            (UnitSystem.Metric, "floz")  => $"{value * 29.57:0.##} ml",
            (UnitSystem.Metric, "lb")    => $"{value * 0.453592:0.##} kg",
            (UnitSystem.Metric, "lbs")   => $"{value * 0.453592:0.##} kg",
            (UnitSystem.Metric, "oz")    => $"{value * 28.35:0.##} g",

            // ── Imperial ──────────────────────────────────────────────────────
            (UnitSystem.Imperial, "g")   => $"{value / 28.35:0.##} oz",
            (UnitSystem.Imperial, "kg")  => $"{value * 2.205:0.##} lbs",
            (UnitSystem.Imperial, "ml")  => $"{value / 29.57:0.##} fl oz",
            (UnitSystem.Imperial, "l")   => $"{value * 33.814:0.##} fl oz",

            // ── Cups ──────────────────────────────────────────────────────────
            (UnitSystem.Cups, "ml")      => $"{value / 240:0.##} cups",
            (UnitSystem.Cups, "l")       => $"{value * 1000 / 240:0.##} cups",
            (UnitSystem.Cups, "g")       => GramsToCups(value, ingredientName),
            (UnitSystem.Cups, "kg")      => GramsToCups(value * 1000, ingredientName),
            (UnitSystem.Cups, "tbsp")    => $"{value / 16:0.##} cups",
            (UnitSystem.Cups, "tsp")     => $"{value / 48:0.##} cups",

            // ── Tbsp ──────────────────────────────────────────────────────────
            (UnitSystem.Tbsp, "ml")      => $"{value / 15:0.##} tbsp",
            (UnitSystem.Tbsp, "l")       => $"{value * 1000 / 15:0.##} tbsp",
            (UnitSystem.Tbsp, "g")       => $"{value / 9:0.##} tbsp",
            (UnitSystem.Tbsp, "kg")      => $"{value * 1000 / 9:0.##} tbsp",
            (UnitSystem.Tbsp, "tsp")     => $"{value / 3:0.##} tbsp",
            (UnitSystem.Tbsp, "cup")     => $"{value * 16:0.##} tbsp",
            (UnitSystem.Tbsp, "cups")    => $"{value * 16:0.##} tbsp",

            _ => numericPart
        };

        // Re-attach any trailing text after the unit token (excluding descriptors already stripped)
        string tail = numericPart[(match.Index + match.Length)..].Trim();
        return tail.Length > 0 ? $"{converted} {tail}" : converted;
    }

    private static string GramsToCups(double grams, string ingredientName)
    {
        // Grams-per-cup for common baking ingredients
        double gramsPerCup = ingredientName.ToLowerInvariant() switch
        {
            var n when n.Contains("flour")  => 120,
            var n when n.Contains("sugar")  => 200,
            var n when n.Contains("butter") => 227,
            var n when n.Contains("rice")   => 185,
            _                               => 0      // unknown — fall back
        };

        return gramsPerCup > 0
            ? $"{grams / gramsPerCup:0.##} cups"
            : $"{grams / 28.35:0.##} oz"; // fall back to oz for unknown ingredients
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
