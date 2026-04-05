using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CookingApp.Core.DTOs;
using CookingApp.Core.Interfaces;
using CookingApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CookingApp.Infrastructure.ExternalServices;

public class AiAssistantService(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    AppDbContext db) : IAiAssistantService
{
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-opus-4-5";

    public async Task<AiChatResponse> ChatAsync(AiChatRequest request)
    {
        // Build system prompt with optional recipe context from DB
        var systemPrompt = await BuildSystemPromptAsync(request.CurrentRecipeId);

        // Build message history for Claude
        var messages = request.History
            .Select(m => new { role = m.Role, content = m.Content })
            .Append(new { role = "user", content = request.NewMessage })
            .ToList();

        var body = new
        {
            model = Model,
            max_tokens = 1024,
            system = systemPrompt,
            messages,
            tools = new[]
            {
                new { type = "web_search_20250305", name = "web_search" }
            }
        };

        var client = httpClientFactory.CreateClient("Anthropic");
        var response = await client.PostAsJsonAsync(AnthropicApiUrl, body);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Extract all text blocks from the response content array
        var reply = string.Join("\n",
            json.GetProperty("content")
                .EnumerateArray()
                .Where(c => c.GetProperty("type").GetString() == "text")
                .Select(c => c.GetProperty("text").GetString() ?? ""));

        return new AiChatResponse(reply);
    }

    private async Task<string> BuildSystemPromptAsync(int? recipeId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a friendly, knowledgeable cooking assistant built into a recipe app.");
        sb.AppendLine("You have access to a web search tool — use it to look up current cooking tips, ingredient info, or techniques when relevant.");
        sb.AppendLine("Keep answers concise and practical. When suggesting ingredient substitutes, be honest about how the dish will change.");

        if (recipeId.HasValue)
        {
            var recipe = await db.Recipes
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == recipeId.Value);

            if (recipe != null)
            {
                sb.AppendLine();
                sb.AppendLine("=== CURRENT RECIPE CONTEXT ===");
                sb.AppendLine($"Recipe: {recipe.Name} ({recipe.Country}, {recipe.Category})");
                sb.AppendLine($"Flavour profile: {recipe.FlavorTags}");
                sb.AppendLine("Ingredients:");
                foreach (var ri in recipe.RecipeIngredients.OrderBy(x => x.SortOrder))
                    sb.AppendLine($"  - {ri.Quantity} {ri.Ingredient.Name}");
                sb.AppendLine($"Instructions:\n{recipe.Instructions}");
                sb.AppendLine("===============================");
                sb.AppendLine("The user is currently viewing this recipe. Tailor your answers to it.");
            }
        }

        return sb.ToString();
    }
}

// ─── Google Places Service ────────────────────────────────────────────────────

public class PlacesService(IHttpClientFactory httpClientFactory, IConfiguration config) : IPlacesService
{
    public async Task<List<NearbyStoreDto>> FindNearbyStoresAsync(
        string ingredientName, double lat, double lng)
    {
        var apiKey = config["GooglePlaces:ApiKey"]
            ?? throw new InvalidOperationException("GooglePlaces:ApiKey not configured");

        // Search for grocery/specialty stores near the user's location
        var query = Uri.EscapeDataString($"{ingredientName} grocery store");
        var url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json" +
                  $"?location={lat},{lng}&radius=3000&keyword={query}" +
                  $"&opennow=false&key={apiKey}";

        var client = httpClientFactory.CreateClient();
        var json = await client.GetFromJsonAsync<JsonElement>(url);

        var results = new List<NearbyStoreDto>();

        if (!json.TryGetProperty("results", out var places)) return results;

        foreach (var place in places.EnumerateArray().Take(5))
        {
            var name = place.GetProperty("name").GetString() ?? "";
            var address = place.TryGetProperty("vicinity", out var v) ? v.GetString() ?? "" : "";
            var isOpen = place.TryGetProperty("opening_hours", out var oh)
                && oh.TryGetProperty("open_now", out var on)
                && on.GetBoolean();

            var placeLocation = place.GetProperty("geometry").GetProperty("location");
            var placeLat = placeLocation.GetProperty("lat").GetDouble();
            var placeLng = placeLocation.GetProperty("lng").GetDouble();
            var distKm = HaversineKm(lat, lng, placeLat, placeLng);

            var placeId = place.TryGetProperty("place_id", out var pid) ? pid.GetString() : null;
            var mapsUrl = placeId != null
                ? $"https://www.google.com/maps/place/?q=place_id:{placeId}"
                : null;

            results.Add(new NearbyStoreDto(name, address, Math.Round(distKm, 1), isOpen, mapsUrl));
        }

        return results.OrderBy(r => r.DistanceKm).ToList();
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
