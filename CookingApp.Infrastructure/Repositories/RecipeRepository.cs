using CookingApp.Core.DTOs;
using CookingApp.Core.Interfaces;
using CookingApp.Core.Models;
using CookingApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CookingApp.Infrastructure.Repositories;

// ─── RecipeRepository ─────────────────────────────────────────────────────────

public class RecipeRepository(AppDbContext db) : IRecipeRepository
{
    public async Task<RecipeDetailDto?> GetByIdAsync(int id)
    {
        var recipe = await db.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null) return null;

        return MapToDetail(recipe);
    }

    public async Task<List<RecipeSummaryDto>> SearchAsync(RecipeSearchRequest req, UserPreferencesDto? prefs)
    {
        var query = db.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .AsQueryable();

        // ── Hard filters ──────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(req.Country))
            query = query.Where(r => EF.Functions.ILike(r.Country, req.Country));

        if (!string.IsNullOrWhiteSpace(req.MainIngredient))
            query = query.Where(r =>
                EF.Functions.ILike(r.MainIngredientName, $"%{req.MainIngredient}%") ||
                r.RecipeIngredients.Any(ri => EF.Functions.ILike(ri.Ingredient.Name, $"%{req.MainIngredient}%")));

        if (!string.IsNullOrWhiteSpace(req.Category))
            query = query.Where(r => r.Category == req.Category);

        if (!string.IsNullOrWhiteSpace(req.Query))
            query = query.Where(r =>
                EF.Functions.ILike(r.Name, $"%{req.Query}%") ||
                EF.Functions.ILike(r.Country, $"%{req.Query}%") ||
                EF.Functions.ILike(r.MainIngredientName, $"%{req.Query}%"));

        if (req.Flavors != null && req.Flavors.Count > 0)
            query = query.Where(r => req.Flavors.Any(f => r.FlavorTags.Contains(f)));

        // ── User preference filtering ─────────────────────────────────────────
        if (prefs != null)
        {
            if (prefs.DislikedFlavors.Count > 0)
                query = query.Where(r =>
                    !prefs.DislikedFlavors.Any(f => r.FlavorTags.Contains(f)));

            if (prefs.DislikedIngredientIds.Count > 0)
                query = query.Where(r =>
                    !r.RecipeIngredients.Any(ri =>
                        prefs.DislikedIngredientIds.Contains(ri.IngredientId)));
        }

        var results = await query
            .OrderBy(r => r.Name)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return results.Select(MapToSummary).ToList();
    }

    public async Task<List<AutocompleteResult>> AutocompleteAsync(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
            return [];

        var countries = await db.Recipes
            .Where(r => EF.Functions.ILike(r.Country, $"{prefix}%"))
            .Select(r => r.Country)
            .Distinct()
            .Take(5)
            .ToListAsync();

        var ingredients = await db.Recipes
            .Where(r => EF.Functions.ILike(r.MainIngredientName, $"{prefix}%"))
            .Select(r => r.MainIngredientName)
            .Distinct()
            .Take(5)
            .ToListAsync();

        var results = new List<AutocompleteResult>();
        results.AddRange(countries.Select(c => new AutocompleteResult(c, "country")));
        results.AddRange(ingredients.Select(i => new AutocompleteResult(i, "ingredient")));
        return results;
    }

    public Task<List<string>> GetAllCountriesAsync() =>
        db.Recipes.Select(r => r.Country).Distinct().OrderBy(c => c).ToListAsync();

    public Task<List<string>> GetAllMainIngredientsAsync() =>
        db.Recipes.Select(r => r.MainIngredientName).Distinct().OrderBy(i => i).ToListAsync();

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static RecipeSummaryDto MapToSummary(Recipe r) => new(
        r.Id, r.Name, r.Country, r.Category, r.MainIngredientName,
        r.FlavorTags, r.PrepTimeMinutes + r.CookTimeMinutes, r.ImageUrl);

    private static RecipeDetailDto MapToDetail(Recipe r) => new(
        r.Id, r.Name, r.Description, r.Country, r.Category,
        r.MainIngredientName, r.FlavorTags, r.Instructions,
        r.PrepTimeMinutes, r.CookTimeMinutes, r.Servings, r.ImageUrl,
        r.RecipeIngredients
            .OrderBy(ri => ri.SortOrder)
            .Select(ri => new RecipeIngredientDto(
                ri.IngredientId, ri.Ingredient.Name,
                ri.Quantity, ri.IsOptional, ri.SortOrder))
            .ToList());
}

// ─── SubstituteRepository ─────────────────────────────────────────────────────

public class SubstituteRepository(AppDbContext db) : ISubstituteRepository
{
    public async Task<List<SubstituteDto>> GetSubstitutesAsync(int ingredientId)
    {
        return await db.IngredientSubstitutes
            .Include(s => s.SubstituteIngredient)
            .Where(s => s.OriginalIngredientId == ingredientId)
            .OrderBy(s => s.ClosenessRank)
            .Select(s => new SubstituteDto(
                s.SubstituteIngredientId,
                s.SubstituteIngredient.Name,
                s.ClosenessRank,
                s.Explanation,
                s.DishImpact))
            .ToListAsync();
    }
}
