using CookingApp.Core.DTOs;
using CookingApp.Core.Interfaces;
using CookingApp.Core.Models;
using CookingApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CookingApp.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(int id) =>
        db.Users.FindAsync(id).AsTask()!;

    public async Task<User> CreateAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public Task<UserPreferences?> GetPreferencesAsync(int userId) =>
        db.UserPreferences
            .Include(p => p.DislikedIngredients)
            .FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task UpsertPreferencesAsync(int userId, UserPreferencesDto dto)
    {
        var prefs = await db.UserPreferences
            .Include(p => p.DislikedIngredients)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs == null)
        {
            prefs = new UserPreferences { UserId = userId };
            db.UserPreferences.Add(prefs);
        }

        prefs.DislikedFlavors = string.Join(',', dto.DislikedFlavors);

        // Replace disliked ingredients (canonical-based)
        db.UserDislikedIngredients.RemoveRange(prefs.DislikedIngredients);
        prefs.DislikedIngredients = dto.DislikedCanonicalIngredientIds
            .Select(id => new UserDislikedIngredient
            {
                UserPreferencesId = prefs.Id,
                CanonicalIngredientId = id
            }).ToList();

        await db.SaveChangesAsync();
    }

    public async Task DislikeIngredientAsync(int userId, int canonicalIngredientId)
    {
        var prefs = await EnsurePreferencesAsync(userId);
        if (prefs.DislikedIngredients.Any(d => d.CanonicalIngredientId == canonicalIngredientId))
            return;

        db.UserDislikedIngredients.Add(new UserDislikedIngredient
        {
            UserPreferencesId = prefs.Id,
            CanonicalIngredientId = canonicalIngredientId
        });
        await db.SaveChangesAsync();
    }

    public async Task UnDislikeIngredientAsync(int userId, int canonicalIngredientId)
    {
        var prefs = await db.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);
        if (prefs == null) return;

        var entry = await db.UserDislikedIngredients
            .FirstOrDefaultAsync(d => d.UserPreferencesId == prefs.Id
                && d.CanonicalIngredientId == canonicalIngredientId);
        if (entry != null)
        {
            db.UserDislikedIngredients.Remove(entry);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<DislikedIngredientDto>> GetDislikedIngredientsAsync(int userId)
    {
        var prefs = await db.UserPreferences
            .Include(p => p.DislikedIngredients)
                .ThenInclude(d => d.CanonicalIngredient)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        return prefs?.DislikedIngredients
            .Select(d => new DislikedIngredientDto(d.CanonicalIngredientId, d.CanonicalIngredient.Name))
            .ToList() ?? [];
    }

    private async Task<UserPreferences> EnsurePreferencesAsync(int userId)
    {
        var prefs = await db.UserPreferences
            .Include(p => p.DislikedIngredients)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs != null) return prefs;

        prefs = new UserPreferences { UserId = userId };
        db.UserPreferences.Add(prefs);
        await db.SaveChangesAsync();
        return prefs;
    }

    public async Task SaveRecipeAsync(int userId, int recipeId)
    {
        var exists = await db.UserSavedRecipes
            .AnyAsync(s => s.UserId == userId && s.RecipeId == recipeId);
        if (exists) return;

        db.UserSavedRecipes.Add(new UserSavedRecipe { UserId = userId, RecipeId = recipeId });
        await db.SaveChangesAsync();
    }

    public async Task UnsaveRecipeAsync(int userId, int recipeId)
    {
        var saved = await db.UserSavedRecipes
            .FirstOrDefaultAsync(s => s.UserId == userId && s.RecipeId == recipeId);
        if (saved != null)
        {
            db.UserSavedRecipes.Remove(saved);
            await db.SaveChangesAsync();
        }
    }

    public Task<bool> IsRecipeSavedAsync(int userId, int recipeId) =>
        db.UserSavedRecipes.AnyAsync(s => s.UserId == userId && s.RecipeId == recipeId);

    public async Task<List<SavedRecipeSummaryDto>> GetSavedRecipesAsync(int userId)
    {
        return await db.UserSavedRecipes
            .Where(s => s.UserId == userId)
            .Include(s => s.Recipe)
            .OrderByDescending(s => s.SavedAt)
            .Select(s => new SavedRecipeSummaryDto(
                s.Recipe.Id,
                s.Recipe.Name,
                s.Recipe.Country,
                s.Recipe.Category,
                s.Recipe.MainIngredientName,
                s.Recipe.FlavorTags,
                s.Recipe.PrepTimeMinutes + s.Recipe.CookTimeMinutes,
                s.Recipe.ImageUrl,
                s.Notes))
            .ToListAsync();
    }

    public async Task UpdateRecipeNotesAsync(int userId, int recipeId, string? notes)
    {
        var saved = await db.UserSavedRecipes
            .FirstOrDefaultAsync(s => s.UserId == userId && s.RecipeId == recipeId);
        if (saved != null)
        {
            saved.Notes = notes;
            await db.SaveChangesAsync();
        }
    }
}
