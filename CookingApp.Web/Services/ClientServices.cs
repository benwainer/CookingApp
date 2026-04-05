using Blazored.LocalStorage;
using Microsoft.JSInterop;

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

        if (!string.IsNullOrEmpty(token))
        {
            IsLoggedIn = true;
            UserId = userId;
            DisplayName = displayName ?? string.Empty;
            NotifyStateChanged();
        }
    }

    public async void Login(string token, int userId, string displayName)
    {
        await storage.SetItemAsync("auth_token", token);
        await storage.SetItemAsync("display_name", displayName);
        await storage.SetItemAsync("user_id", userId);

        IsLoggedIn = true;
        UserId = userId;
        DisplayName = displayName;
        NotifyStateChanged();
    }

    public async void Logout()
    {
        await storage.RemoveItemAsync("auth_token");
        await storage.RemoveItemAsync("display_name");
        await storage.RemoveItemAsync("user_id");

        IsLoggedIn = false;
        UserId = 0;
        DisplayName = string.Empty;
        NotifyStateChanged();
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
