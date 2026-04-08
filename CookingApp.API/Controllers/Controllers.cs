using System.Security.Claims;
using CookingApp.Core.DTOs;
using CookingApp.Core.Interfaces;
using CookingApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookingApp.API.Controllers;

// ─── RecipesController ────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public class RecipesController(IRecipeService recipeService, AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] RecipeSearchRequest request)
    {
        var userId = GetUserIdOrNull();
        var results = await recipeService.SearchAsync(request, userId);
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var recipe = await recipeService.GetDetailAsync(id);
        return recipe == null ? NotFound() : Ok(recipe);
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string prefix)
    {
        var results = await recipeService.AutocompleteAsync(prefix);
        return Ok(results);
    }

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries()
    {
        var countries = await recipeService.GetAllCountriesAsync();
        return Ok(countries);
    }

    [HttpGet("main-ingredients")]
    public async Task<IActionResult> GetMainIngredients()
    {
        var ingredients = await recipeService.GetAllMainIngredientsAsync();
        return Ok(ingredients);
    }

    [HttpGet("ingredients-by-category")]
    public async Task<IActionResult> GetIngredientsByCategory()
    {
        var result = await recipeService.GetIngredientsByCategoryAsync();
        return Ok(result);
    }

    [HttpGet("canonical-ingredients")]
    public async Task<IActionResult> GetCanonicalIngredients()
    {
        var categoryOrder = new[] { "protein", "seafood", "legume", "vegetable", "grain" };
        var raw = await db.CanonicalIngredients
            .Where(c => categoryOrder.Contains(c.Category))
            .OrderBy(c => c.Name)
            .GroupBy(c => c.Category)
            .ToDictionaryAsync(g => g.Key, g => g.Select(c => c.Name).OrderBy(n => n).ToList());

        // Return in fixed category order, not DB/alphabetical order
        var result = categoryOrder
            .Where(raw.ContainsKey)
            .ToDictionary(cat => cat, cat => raw[cat]);

        return Ok(result);
    }

    private int? GetUserIdOrNull()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? int.Parse(claim) : null;
    }
}

// ─── SubstitutesController ────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public class SubstitutesController(ISubstituteService substituteService) : ControllerBase
{
    /// <summary>
    /// Returns the next substitute not yet shown to the user.
    /// Pass comma-separated already-suggested ranks as query param.
    /// e.g. GET /api/substitutes/4?suggested=2,3
    /// </summary>
    [HttpGet("{ingredientId:int}")]
    public async Task<IActionResult> GetNext(int ingredientId, [FromQuery] string? suggested)
    {
        var alreadySuggested = suggested?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToList() ?? [];

        var sub = await substituteService.GetNextSubstituteAsync(ingredientId, alreadySuggested);

        // Null = no more substitutes available
        return Ok(sub); // null is serialised as JSON null — client handles it
    }
}

// ─── AuthController ───────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserService userService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var result = await userService.RegisterAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var result = await userService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}

// ─── UsersController ──────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var prefs = await userService.GetPreferencesAsync(GetUserId());
        return Ok(prefs ?? new UserPreferencesDto([], []));
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> SavePreferences(UserPreferencesDto dto)
    {
        await userService.SavePreferencesAsync(GetUserId(), dto);
        return NoContent();
    }

    [HttpGet("saved-recipes")]
    public async Task<IActionResult> GetSavedRecipes()
    {
        var recipes = await userService.GetSavedRecipesAsync(GetUserId());
        return Ok(recipes);
    }

    [HttpGet("saved-recipes/{recipeId:int}")]
    public async Task<IActionResult> IsRecipeSaved(int recipeId)
    {
        var saved = await userService.IsRecipeSavedAsync(GetUserId(), recipeId);
        return Ok(saved);
    }

    [HttpPost("saved-recipes/{recipeId:int}")]
    public async Task<IActionResult> SaveRecipe(int recipeId)
    {
        await userService.SaveRecipeAsync(GetUserId(), recipeId);
        return NoContent();
    }

    [HttpDelete("saved-recipes/{recipeId:int}")]
    public async Task<IActionResult> UnsaveRecipe(int recipeId)
    {
        await userService.UnsaveRecipeAsync(GetUserId(), recipeId);
        return NoContent();
    }

    [HttpPatch("saved-recipes/{recipeId:int}/notes")]
    public async Task<IActionResult> UpdateRecipeNotes(int recipeId, UpdateRecipeNotesRequest request)
    {
        await userService.UpdateRecipeNotesAsync(GetUserId(), recipeId, request.Notes);
        return NoContent();
    }

    [HttpGet("disliked-ingredients")]
    public async Task<IActionResult> GetDislikedIngredients()
    {
        var result = await userService.GetDislikedIngredientsAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("dislike-ingredient/{canonicalIngredientId:int}")]
    public async Task<IActionResult> DislikeIngredient(int canonicalIngredientId)
    {
        await userService.DislikeIngredientAsync(GetUserId(), canonicalIngredientId);
        return NoContent();
    }

    [HttpDelete("dislike-ingredient/{canonicalIngredientId:int}")]
    public async Task<IActionResult> UnDislikeIngredient(int canonicalIngredientId)
    {
        await userService.UnDislikeIngredientAsync(GetUserId(), canonicalIngredientId);
        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

// ─── AiController ─────────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController(IAiAssistantService aiService) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<IActionResult> Chat(AiChatRequest request)
    {
        var response = await aiService.ChatAsync(request);
        return Ok(response);
    }
}

// ─── StoresController ─────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public class StoresController(IPlacesService placesService) : ControllerBase
{
    [HttpGet("nearby")]
    public async Task<IActionResult> Nearby(
        [FromQuery] string ingredient,
        [FromQuery] double lat,
        [FromQuery] double lng)
    {
        var stores = await placesService.FindNearbyStoresAsync(ingredient, lat, lng);
        return Ok(stores);
    }
}
