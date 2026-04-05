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

    protected override async Task OnInitializedAsync()
    {
        recipe = await Api.GetRecipeAsync(Id);
        isLoading = false;
    }

    private async Task RequestSubstitute(int ingredientId, string ingredientName)
    {
        if (!substituteState.ContainsKey(ingredientId))
            substituteState[ingredientId] = new SubstituteState();

        if (!suggestedRanks.ContainsKey(ingredientId))
            suggestedRanks[ingredientId] = [];

        substituteState[ingredientId].IsLoading = true;
        StateHasChanged();

        var sub = await Api.GetNextSubstituteAsync(ingredientId, suggestedRanks[ingredientId]);

        if (sub != null)
            suggestedRanks[ingredientId].Add(sub.ClosenessRank);

        substituteState[ingredientId] = new SubstituteState { Sub = sub, IsLoading = false };
        StateHasChanged();
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
