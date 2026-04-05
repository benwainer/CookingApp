// ── CookingApp.Importer ────────────────────────────────────────────────────────
// Fetches recipes from TheMealDB and inserts them into the CookingApp database.
//
// Before running, set the COOKINGAPP_DB environment variable with your
// PostgreSQL connection string. For example, in PowerShell:
//
//   $env:COOKINGAPP_DB = "Host=localhost;Port=5432;Database=cookingapp;Username=postgres;Password=yourpassword"
//
// Or to save it permanently so you don't have to set it every session:
//
//   [System.Environment]::SetEnvironmentVariable("COOKINGAPP_DB", "Host=...", "User")
// ──────────────────────────────────────────────────────────────────────────────

using System.Text.Json;
using Dapper;
using Npgsql;

var ConnectionString =
    Environment.GetEnvironmentVariable("COOKINGAPP_DB")
    ?? "Host=localhost;Port=5432;Database=cookingapp;Username=postgres;Password=";

const string ApiBase = "https://www.themealdb.com/api/json/v1/1";

// ── Category mapping: MealDB category → app category ──────────────────────────
static string MapCategory(string mealDbCategory) => mealDbCategory switch
{
    "Breakfast"                                         => "Breakfast",
    "Starter" or "Side"                                 => "Starter",
    "Dessert" or "Cake" or "Pudding" or "Pastry"        => "Dessert",
    "Snack" or "Vegetarian"                             => "Snack",
    _                                                   => "Main"
};

// ── Flavor tags: rough heuristic from area + category ─────────────────────────
static string MapFlavorTags(string area, string appCategory) =>
    appCategory == "Dessert" ? "sweet" :
    appCategory == "Breakfast" ? "savory" :
    area is "Indian" or "Moroccan" or "Mexican" or "Thai"
           or "Jamaican" or "Malaysian" or "Filipino"   ? "spicy,savory,aromatic" :
    area is "Japanese" or "Chinese" or "Vietnamese"
           or "Korean"                                  ? "savory,aromatic" :
    area is "Italian" or "Greek" or "French"
           or "Spanish" or "Portuguese"                 ? "savory,aromatic" :
    "savory";

// ── Find-or-create an ingredient row, return its Id ───────────────────────────
static async Task<int> GetOrCreateIngredientAsync(NpgsqlConnection conn, string name)
{
    var existing = await conn.ExecuteScalarAsync<int?>(
        """SELECT "Id" FROM "Ingredients" WHERE "Name" = @Name""",
        new { Name = name });

    if (existing.HasValue) return existing.Value;

    return await conn.ExecuteScalarAsync<int>(
        """
        INSERT INTO "Ingredients" ("Name", "Category", "Notes")
        VALUES (@Name, 'other', NULL)
        RETURNING "Id"
        """,
        new { Name = name });
}

// ── Main ──────────────────────────────────────────────────────────────────────
using var http = new HttpClient();
http.DefaultRequestHeaders.Add("User-Agent", "CookingApp-Importer/1.0");

await using var conn = new NpgsqlConnection(ConnectionString);
await conn.OpenAsync();

Console.WriteLine("Fetching categories from TheMealDB...");
await Task.Delay(200);

var categoriesJson = await http.GetStringAsync($"{ApiBase}/categories.php");
var categoriesDoc  = JsonDocument.Parse(categoriesJson);
var categories     = categoriesDoc.RootElement
    .GetProperty("categories")
    .EnumerateArray()
    .Select(c => c.GetProperty("strCategory").GetString()!)
    .ToList();

Console.WriteLine($"Found {categories.Count} categories.\n");

int inserted = 0, skipped = 0, errors = 0;

foreach (var category in categories)
{
    Console.WriteLine($"── {category} ──");
    await Task.Delay(200);

    // Fetch meal stubs for this category
    var listJson = await http.GetStringAsync(
        $"{ApiBase}/filter.php?c={Uri.EscapeDataString(category)}");
    var listDoc = JsonDocument.Parse(listJson);

    if (!listDoc.RootElement.TryGetProperty("meals", out var mealsEl)
        || mealsEl.ValueKind == JsonValueKind.Null)
    {
        Console.WriteLine("  (no meals)\n");
        continue;
    }

    var mealIds = mealsEl.EnumerateArray()
        .Select(m => m.GetProperty("idMeal").GetString()!)
        .ToList();

    Console.WriteLine($"  {mealIds.Count} meals found.");

    foreach (var mealId in mealIds)
    {
        await Task.Delay(200);

        try
        {
            // Fetch full details
            var detailJson = await http.GetStringAsync($"{ApiBase}/lookup.php?i={mealId}");
            var detailDoc  = JsonDocument.Parse(detailJson);
            var meal       = detailDoc.RootElement.GetProperty("meals")[0];

            var name = meal.GetProperty("strMeal").GetString() ?? "";

            // Skip duplicates by name
            var exists = await conn.ExecuteScalarAsync<int>(
                """SELECT COUNT(*) FROM "Recipes" WHERE "Name" = @Name""",
                new { Name = name });

            if (exists > 0)
            {
                Console.WriteLine($"  [SKIP] {name}");
                skipped++;
                continue;
            }

            var area         = meal.GetProperty("strArea").GetString() ?? "";
            var instructions = meal.GetProperty("strInstructions").GetString() ?? "";
            var imageUrl     = meal.GetProperty("strMealThumb").GetString() ?? "";
            var appCategory  = MapCategory(category);
            var flavorTags   = MapFlavorTags(area, appCategory);

            // Collect non-empty ingredient + measure pairs (up to 20)
            var pairs = new List<(string Name, string Measure)>();
            for (int i = 1; i <= 20; i++)
            {
                var ingName = meal.TryGetProperty($"strIngredient{i}", out var ip)
                    ? ip.GetString()?.Trim() : null;
                var measure = meal.TryGetProperty($"strMeasure{i}", out var mp)
                    ? mp.GetString()?.Trim() : null;

                if (!string.IsNullOrWhiteSpace(ingName))
                    pairs.Add((ingName, measure ?? ""));
            }

            var mainIngredient = pairs.Count > 0 ? pairs[0].Name : "";

            // Insert recipe
            var recipeId = await conn.ExecuteScalarAsync<int>(
                """
                INSERT INTO "Recipes"
                    ("Name","Description","Country","Category","MainIngredientName",
                     "FlavorTags","Instructions","PrepTimeMinutes","CookTimeMinutes",
                     "Servings","ImageUrl","CreatedAt")
                VALUES
                    (@Name,@Description,@Country,@Category,@MainIngredientName,
                     @FlavorTags,@Instructions,@PrepTimeMinutes,@CookTimeMinutes,
                     @Servings,@ImageUrl,@CreatedAt)
                RETURNING "Id"
                """,
                new
                {
                    Name               = name,
                    Description        = $"{(string.IsNullOrWhiteSpace(area) ? "" : area + " ")}{category} dish.",
                    Country            = area,
                    Category           = appCategory,
                    MainIngredientName = mainIngredient,
                    FlavorTags         = flavorTags,
                    Instructions       = instructions,
                    PrepTimeMinutes    = 15,
                    CookTimeMinutes    = 30,
                    Servings           = 4,
                    ImageUrl           = imageUrl,
                    CreatedAt          = DateTime.UtcNow
                });

            // Insert ingredients and link rows
            for (int i = 0; i < pairs.Count; i++)
            {
                var (ingName, measure) = pairs[i];
                var ingId = await GetOrCreateIngredientAsync(conn, ingName);

                await conn.ExecuteAsync(
                    """
                    INSERT INTO "RecipeIngredients"
                        ("RecipeId","IngredientId","Quantity","IsOptional","SortOrder")
                    VALUES (@RecipeId,@IngredientId,@Quantity,false,@SortOrder)
                    ON CONFLICT DO NOTHING
                    """,
                    new { RecipeId = recipeId, IngredientId = ingId, Quantity = measure, SortOrder = i + 1 });
            }

            Console.WriteLine($"  [OK]   {name} ({area}, {pairs.Count} ingredients)");
            inserted++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ERR]  meal {mealId}: {ex.Message}");
            errors++;
        }
    }

    Console.WriteLine();
}

Console.WriteLine("═══════════════════════════════");
Console.WriteLine("  Import complete");
Console.WriteLine($"  Inserted : {inserted}");
Console.WriteLine($"  Skipped  : {skipped}  (already existed)");
Console.WriteLine($"  Errors   : {errors}");
Console.WriteLine("═══════════════════════════════");
