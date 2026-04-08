using Blazored.LocalStorage;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CookingApp.Web.Services;

/// <summary>
/// Holds the current user's auth state in memory for the Blazor session.
/// On login, also persists the JWT token to LocalStorage so it survives page refreshes.
/// </summary>
public class AuthStateService(ILocalStorageService storage)
{
    public bool IsLoggedIn { get; private set; }
    public int UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var token = await storage.GetItemAsync<string>("auth_token");
        var displayName = await storage.GetItemAsync<string>("display_name");
        var userId = await storage.GetItemAsync<int>("user_id");

        if (!string.IsNullOrEmpty(token) && !IsTokenExpired(token))
        {
            IsLoggedIn = true;
            UserId = userId;
            DisplayName = displayName ?? string.Empty;
            NotifyStateChanged();
        }
        else if (!string.IsNullOrEmpty(token))
        {
            // Token expired — clear storage
            await storage.RemoveItemAsync("auth_token");
            await storage.RemoveItemAsync("display_name");
            await storage.RemoveItemAsync("user_id");
        }
    }

    public async Task LoginAsync(string token, int userId, string displayName)
    {
        await storage.SetItemAsync("auth_token", token);
        await storage.SetItemAsync("display_name", displayName);
        await storage.SetItemAsync("user_id", userId);

        IsLoggedIn = true;
        UserId = userId;
        DisplayName = displayName;
        NotifyStateChanged();
    }

    public async Task LogoutAsync()
    {
        await storage.RemoveItemAsync("auth_token");
        await storage.RemoveItemAsync("display_name");
        await storage.RemoveItemAsync("user_id");

        IsLoggedIn = false;
        UserId = 0;
        DisplayName = string.Empty;
        NotifyStateChanged();
    }

    private static bool IsTokenExpired(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return true;

            var payload = parts[1];
            // Pad base64url to standard base64
            var padded = payload.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');

            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("exp", out var expEl))
            {
                var exp = expEl.GetInt64();
                var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);
                return expiry <= DateTimeOffset.UtcNow;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

/// <summary>
/// Gets the user's browser geolocation via JS interop.
/// Returns a default (Tel Aviv) if location is denied.
/// </summary>
public class UserLocationService(Microsoft.JSInterop.IJSRuntime js)
{
    private (double Lat, double Lng)? cached;

    public async Task<(double Lat, double Lng)> GetCoordinatesAsync()
    {
        if (cached.HasValue) return cached.Value;

        try
        {
            var coords = await js.InvokeAsync<double[]>("getLocation");
            cached = (coords[0], coords[1]);
            return cached.Value;
        }
        catch
        {
            // Default: Tel Aviv
            return (32.0853, 34.7818);
        }
    }
}
