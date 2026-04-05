using CookingApp.Core.DTOs;
using CookingApp.Core.Interfaces;
using CookingApp.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CookingApp.Core.Services;

// ─── RecipeService ────────────────────────────────────────────────────────────

public class RecipeService(IRecipeRepository repo, IUserRepository userRepo) : IRecipeService
{
    public async Task<List<RecipeSummaryDto>> SearchAsync(RecipeSearchRequest request, int? userId)
    {
        UserPreferencesDto? prefs = null;
        if (userId.HasValue)
        {
            var rawPrefs = await userRepo.GetPreferencesAsync(userId.Value);
            if (rawPrefs != null)
                prefs = new UserPreferencesDto(
                rawPrefs.DislikedFlavors.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                rawPrefs.DislikedIngredients.Select(d => d.IngredientId).ToList()
            );
        }

        return await repo.SearchAsync(request, prefs);
    }

    public Task<RecipeDetailDto?> GetDetailAsync(int id) => repo.GetByIdAsync(id);

    public Task<List<AutocompleteResult>> AutocompleteAsync(string prefix) =>
        repo.AutocompleteAsync(prefix);
}

// ─── SubstituteService ────────────────────────────────────────────────────────

public class SubstituteService(ISubstituteRepository repo) : ISubstituteService
{
    public async Task<SubstituteDto?> GetNextSubstituteAsync(int ingredientId, List<int> alreadySuggestedRanks)
    {
        var all = await repo.GetSubstitutesAsync(ingredientId);

        // Return the lowest rank not yet suggested
        return all.FirstOrDefault(s => !alreadySuggestedRanks.Contains(s.ClosenessRank));
    }
}

// ─── UserService ──────────────────────────────────────────────────────────────

public class UserService(IUserRepository repo, IConfiguration config) : IUserService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await repo.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            DisplayName = request.DisplayName,
            PasswordHash = HashPassword(request.Password)
        };

        var created = await repo.CreateAsync(user);
        return new AuthResponse(GenerateJwt(created), created.Id, created.DisplayName);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await repo.GetByEmailAsync(request.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return new AuthResponse(GenerateJwt(user), user.Id, user.DisplayName);
    }

    public async Task<UserPreferencesDto?> GetPreferencesAsync(int userId)
    {
        var prefs = await repo.GetPreferencesAsync(userId);
        if (prefs == null) return null;

        return new UserPreferencesDto(
            prefs.DislikedFlavors.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            prefs.DislikedIngredients.Select(d => d.IngredientId).ToList()
        );
    }

    public Task SavePreferencesAsync(int userId, UserPreferencesDto dto) =>
        repo.UpsertPreferencesAsync(userId, dto);

    public Task SaveRecipeAsync(int userId, int recipeId) =>
        repo.SaveRecipeAsync(userId, recipeId);

    public Task UnsaveRecipeAsync(int userId, int recipeId) =>
        repo.UnsaveRecipeAsync(userId, recipeId);

    public Task<List<RecipeSummaryDto>> GetSavedRecipesAsync(int userId) =>
        repo.GetSavedRecipesAsync(userId);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, 200_000,
            HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, 200_000,
            HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret not configured")));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
