using System.Security.Claims;
using CookingApp.Core.DTOs;
using CookingApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookingApp.API.Controllers;

// ─── RecipesController ────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public class RecipesController(IRecipeService recipeService) : ControllerBase
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

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

// ─── AiController ─────────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
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
