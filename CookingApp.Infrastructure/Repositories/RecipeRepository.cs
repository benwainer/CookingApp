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
        {
            var canonical = await db.CanonicalIngredients
                .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Name, req.MainIngredient));

            if (canonical != null)
            {
                var matchingIngredientIds = await db.Ingredients
                    .Where(i => i.CanonicalIngredientId == canonical.Id)
                    .Select(i => i.Id)
                    .ToListAsync();

                query = query.Where(r =>
                    r.RecipeIngredients.Any(ri => matchingIngredientIds.Contains(ri.IngredientId)));
            }
            else
            {
                query = query.Where(r =>
                    EF.Functions.ILike(r.MainIngredientName, $"%{req.MainIngredient}%") ||
                    r.RecipeIngredients.Any(ri => EF.Functions.ILike(ri.Ingredient.Name, $"%{req.MainIngredient}%")));
            }
        }

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

            if (prefs.DislikedCanonicalIngredientIds.Count > 0)
                query = query.Where(r =>
                    !r.RecipeIngredients.Any(ri =>
                        ri.Ingredient.CanonicalIngredientId != null &&
                        prefs.DislikedCanonicalIngredientIds.Contains(ri.Ingredient.CanonicalIngredientId.Value)));
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
            .Take(4)
            .ToListAsync();

        var ingredients = await db.Recipes
            .Where(r => EF.Functions.ILike(r.MainIngredientName, $"{prefix}%"))
            .Select(r => r.MainIngredientName)
            .Distinct()
            .Take(4)
            .ToListAsync();

        var recipeNames = await db.Recipes
            .Where(r => EF.Functions.ILike(r.Name, $"{prefix}%"))
            .Select(r => r.Name)
            .Distinct()
            .Take(4)
            .ToListAsync();

        var results = new List<AutocompleteResult>();
        results.AddRange(countries.Select(c => new AutocompleteResult(c, "country")));
        results.AddRange(ingredients.Select(i => new AutocompleteResult(i, "ingredient")));
        results.AddRange(recipeNames.Select(n => new AutocompleteResult(n, "recipe")));
        return results;
    }

    public Task<List<string>> GetAllCountriesAsync() =>
        db.Recipes.Select(r => r.Country).Distinct().OrderBy(c => c).ToListAsync();

    public async Task<Dictionary<string, List<string>>> GetIngredientsByCategoryAsync()
    {
        return await db.Recipes
            .Join(db.RecipeIngredients,
                r  => r.Id,
                ri => ri.RecipeId,
                (r, ri) => new { r, ri })
            .Join(db.Ingredients,
                x  => x.ri.IngredientId,
                i  => i.Id,
                (x, i) => new { x.r, i })
            .Where(x => x.r.MainIngredientName == x.i.Name && x.i.CanonicalIngredientId != null)
            .GroupBy(x => x.i.Category)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.r.MainIngredientName).Distinct().OrderBy(n => n).ToList());
    }

    public async Task<List<string>> GetAllMainIngredientsAsync()
    {
        return await db.Recipes
            .Join(db.RecipeIngredients,
                r  => r.Id,
                ri => ri.RecipeId,
                (r, ri) => new { r, ri })
            .Where(x => x.r.MainIngredientName == x.ri.Ingredient.Name
                && (x.ri.Quantity.ToLower().Contains("g ")
                    || x.ri.Quantity.ToLower().Contains("kg")
                    || x.ri.Quantity.ToLower().EndsWith("g")
                    || x.ri.Quantity.ToLower().Contains("gram")))
            .GroupBy(x => x.r.MainIngredientName.ToLower().Trim())
            .Where(g => g.Count() >= 2)
            .Select(g => g.First().r.MainIngredientName.Trim())
            .OrderBy(n => n)
            .ToListAsync();
    }

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
                ri.Quantity, ri.IsOptional, ri.SortOrder,
                ri.Ingredient.CanonicalIngredientId))
            .ToList());
}

// ─── SubstituteRepository ─────────────────────────────────────────────────────

public class SubstituteRepository(AppDbContext db) : ISubstituteRepository
{
    public async Task<List<SubstituteDto>> GetSubstitutesAsync(int ingredientId)
    {
        async Task<List<SubstituteDto>> GetSubsForIds(List<int> ids) =>
            await db.IngredientSubstitutes
                .Include(s => s.SubstituteIngredient)
                .Where(s => ids.Contains(s.OriginalIngredientId))
                .OrderBy(s => s.ClosenessRank)
                .Select(s => new SubstituteDto(
                    s.SubstituteIngredientId,
                    s.SubstituteIngredient.Name,
                    s.ClosenessRank,
                    s.Explanation,
                    s.DishImpact))
                .ToListAsync();

        // Step 1: exact match by ingredientId
        var exact = await GetSubsForIds([ingredientId]);
        if (exact.Count > 0) return exact;

        // Step 2: via CanonicalIngredientId
        var ingredient = await db.Ingredients
            .Include(i => i.CanonicalIngredient)
            .FirstOrDefaultAsync(i => i.Id == ingredientId);

        if (ingredient == null) return [];

        if (ingredient.CanonicalIngredientId.HasValue)
        {
            var siblingIds = await db.Ingredients
                .Where(i => i.CanonicalIngredientId == ingredient.CanonicalIngredientId)
                .Select(i => i.Id)
                .ToListAsync();

            var canonicalSubs = await GetSubsForIds(siblingIds);
            if (canonicalSubs.Count > 0) return canonicalSubs;
        }

        var nameParts = ingredient.Name.ToLower().Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Step 3: strip from the FRONT one word at a time
        // "extra virgin olive oil" → "virgin olive oil" → "olive oil" → "oil"
        for (int startIdx = 1; startIdx < nameParts.Length; startIdx++)
        {
            var suffix = string.Join(' ', nameParts.Skip(startIdx));
            var suffixMatch = await db.CanonicalIngredients
                .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Name, suffix));

            if (suffixMatch == null) continue;

            var suffixIds = await db.Ingredients
                .Where(i => i.CanonicalIngredientId == suffixMatch.Id)
                .Select(i => i.Id)
                .ToListAsync();
            var suffixSubs = await GetSubsForIds(suffixIds);
            if (suffixSubs.Count > 0) return suffixSubs;
        }

        // Step 4: strip from the END one word at a time
        // "garlic clove" → "garlic"  /  "chicken breast fillet" → "chicken breast" → "chicken"
        for (int endIdx = nameParts.Length - 1; endIdx >= 1; endIdx--)
        {
            var prefix = string.Join(' ', nameParts.Take(endIdx));
            var prefixMatch = await db.CanonicalIngredients
                .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Name, prefix));

            if (prefixMatch == null) continue;

            var prefixIds = await db.Ingredients
                .Where(i => i.CanonicalIngredientId == prefixMatch.Id)
                .Select(i => i.Id)
                .ToListAsync();
            var prefixSubs = await GetSubsForIds(prefixIds);
            if (prefixSubs.Count > 0) return prefixSubs;
        }

        return [];
    }
}
