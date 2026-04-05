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

        // Replace disliked ingredients
        db.UserDislikedIngredients.RemoveRange(prefs.DislikedIngredients);
        prefs.DislikedIngredients = dto.DislikedIngredientIds
            .Select(id => new UserDislikedIngredient
            {
                UserPreferencesId = prefs.Id,
                IngredientId = id
            }).ToList();

        await db.SaveChangesAsync();
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

    public async Task<List<RecipeSummaryDto>> GetSavedRecipesAsync(int userId)
    {
        return await db.UserSavedRecipes
            .Where(s => s.UserId == userId)
            .Include(s => s.Recipe)
            .OrderByDescending(s => s.SavedAt)
            .Select(s => new RecipeSummaryDto(
                s.Recipe.Id,
                s.Recipe.Name,
                s.Recipe.Country,
                s.Recipe.Category,
                s.Recipe.MainIngredientName,
                s.Recipe.FlavorTags,
                s.Recipe.PrepTimeMinutes + s.Recipe.CookTimeMinutes,
                s.Recipe.ImageUrl))
            .ToListAsync();
    }
}
