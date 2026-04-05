using CookingApp.Core.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CookingApp.Web.Components.Pages;

public partial class Home
{
    private string searchQuery = string.Empty;
    private List<AutocompleteResult> suggestions = [];
    private List<string> selectedFlavors = [];
    private System.Timers.Timer? debounceTimer;

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        searchQuery = e.Value?.ToString() ?? string.Empty;

        // Debounce: wait 250ms after last keystroke before hitting the API
        debounceTimer?.Dispose();
        debounceTimer = new System.Timers.Timer(250);
        debounceTimer.Elapsed += async (_, _) =>
        {
            debounceTimer?.Dispose();
            if (searchQuery.Length >= 2)
            {
                suggestions = await Api.GetAutocompleteAsync(searchQuery);
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                suggestions = [];
                await InvokeAsync(StateHasChanged);
            }
        };
        debounceTimer.AutoReset = false;
        debounceTimer.Start();
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") DoSearch();
        if (e.Key == "Escape") suggestions = [];
    }

    private void SelectSuggestion(AutocompleteResult suggestion)
    {
        searchQuery = suggestion.Value;
        suggestions = [];

        // Navigate directly based on type
        if (suggestion.Type == "country")
            Nav.NavigateTo($"/browse?country={Uri.EscapeDataString(suggestion.Value)}");
        else
            Nav.NavigateTo($"/browse?ingredient={Uri.EscapeDataString(suggestion.Value)}");
    }

    private void DoSearch()
    {
        suggestions = [];
        if (!string.IsNullOrWhiteSpace(searchQuery))
            Nav.NavigateTo($"/search?q={Uri.EscapeDataString(searchQuery)}");
    }

    private void ToggleFlavor(string flavor)
    {
        if (selectedFlavors.Contains(flavor))
            selectedFlavors.Remove(flavor);
        else
            selectedFlavors.Add(flavor);
    }

    private void BrowseByFlavor()
    {
        var flavorsParam = string.Join(",", selectedFlavors);
        Nav.NavigateTo($"/browse?flavors={Uri.EscapeDataString(flavorsParam)}");
    }

    private static string FlavorEmoji(string flavor) => flavor switch
    {
        "spicy"    => "🌶️",
        "sweet"    => "🍬",
        "sour"     => "🍋",
        "salty"    => "🧂",
        "bitter"   => "☕",
        "savory"   => "🫕",
        "crispy"   => "✨",
        "aromatic" => "🌿",
        _          => "🍴"
    };
}
