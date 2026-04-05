# CookingApp

A full-stack recipe discovery app built with **C# / .NET 8**, **Blazor Server**, **PostgreSQL**, and the **Claude API**.

---

## Solution Structure

```
CookingApp.sln
├── CookingApp.Core          # Domain models, DTOs, interfaces, business logic services
├── CookingApp.Infrastructure # EF Core DbContext, repositories, external API clients
├── CookingApp.API           # ASP.NET Core Web API (controllers, JWT auth)
└── CookingApp.Web           # Blazor Server frontend
```

---

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| PostgreSQL | 15+ | https://www.postgresql.org/download |
| Visual Studio 2022 | 17.8+ | or VS Code with C# Dev Kit |

---

## First-Time Setup

### 1. Clone / open the solution

Open `CookingApp.sln` in Visual Studio or run `code .` in the repo root.

### 2. Configure secrets

Edit **`CookingApp.API/appsettings.json`**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cookingapp;Username=postgres;Password=YOUR_PG_PASSWORD"
  },
  "Jwt": {
    "Secret": "REPLACE_WITH_32_PLUS_CHAR_RANDOM_STRING",
    "Issuer": "CookingApp",
    "Audience": "CookingApp"
  },
  "Anthropic": {
    "ApiKey": "sk-ant-..."
  },
  "GooglePlaces": {
    "ApiKey": "AIza..."
  }
}
```

> **Tip:** For local dev, use `dotnet user-secrets` instead of editing appsettings.json so secrets never get committed to git.

### 3. Create the database

```bash
# From the repo root
cd CookingApp.API
dotnet ef database update
```

This runs all migrations and seeds the initial recipe data automatically.

If you haven't installed the EF tools yet:
```bash
dotnet tool install --global dotnet-ef
```

### 4. Run both projects

**Option A — Visual Studio:** Set multiple startup projects:
- Right-click the solution → Properties → Multiple startup projects
- Set `CookingApp.API` and `CookingApp.Web` both to **Start**
- Press F5

**Option B — Two terminals:**

Terminal 1 (API):
```bash
cd CookingApp.API
dotnet run
# Listening on https://localhost:7001
```

Terminal 2 (Web):
```bash
cd CookingApp.Web
dotnet run
# Listening on https://localhost:7002
```

Then open **https://localhost:7002** in your browser.

---

## Feature Guide

### Search & Browse
- **Home page** — search bar with live autocomplete for countries and ingredients (debounced, triggers after 2 chars)
- **Browse page** — filter grid by country, ingredient, category, or flavour
- **Flavour browse** — click spicy / sweet / sour etc. on the home page to filter by flavour profile

### Recipe Detail
- Full ingredient list with quantities
- **🔄 I don't have this** — click on any ingredient to get a substitute suggestion. Click again to get the next one. When all substitutes are exhausted the app explains what will be different without that ingredient.
- **🏪 Find nearby store** — clicks the browser's geolocation API and queries Google Places for stores carrying that ingredient within 3km.
- **❤️ Save Recipe** — saves to your account (requires login)

### User Accounts
- Register / login with email + password (JWT auth, bcrypt-style PBKDF2 hashing)
- **Preferences page** — mark flavours and ingredients you dislike. All recipe searches automatically exclude them.

### AI Cooking Assistant
- Floating **🤖** button always visible in the bottom-right corner
- When you're on a recipe page, the assistant automatically receives the full recipe as context
- Uses the **Claude API with web search** enabled, so it can look up current information

---

## Adding More Recipes

Insert directly into the PostgreSQL database, or add to the `SeedData` method in:
```
CookingApp.Infrastructure/Data/AppDbContext.cs
```

Then run `dotnet ef database update` again.

---

## Adding Ingredient Substitutes

Add rows to `IngredientSubstitutes` with:
- `OriginalIngredientId` — the ingredient being replaced
- `SubstituteIngredientId` — the replacement
- `ClosenessRank` — 1 (identical) → 4 (last resort)
- `Explanation` — shown to the user
- `DishImpact` — optional warning about how the dish changes

The substitute chain is served in rank order. When the user clicks "I don't have this" repeatedly, ranks 1 → 2 → 3 → 4 are shown in sequence, then null (no more substitutes).

---

## Project Architecture Explained

### Why four projects?

| Project | Depends on | Purpose |
|---------|-----------|---------|
| `Core` | nothing | Pure C# — models, interfaces, business rules. No framework deps. |
| `Infrastructure` | Core | All I/O — database, HTTP clients. Implements Core interfaces. |
| `API` | Core + Infrastructure | HTTP layer only. Wires up DI, handles auth, calls services. |
| `Web` | Core only | UI layer. Calls API over HTTP. Knows nothing about the DB. |

This is the standard **Clean Architecture** pattern. The key rule: dependencies only point inward (toward Core). This means you can swap PostgreSQL for another database by only changing Infrastructure, with zero changes elsewhere.

### How a recipe search works end-to-end

```
User types in Blazor search bar
  → (debounce 250ms)
  → ApiClient.GetAutocompleteAsync()
  → GET /api/recipes/autocomplete?prefix=Ind
  → RecipesController.Autocomplete()
  → RecipeService.AutocompleteAsync()
  → RecipeRepository.AutocompleteAsync()     ← EF Core ILike query on PostgreSQL
  → returns [{value:"India", type:"country"}, ...]
  → displayed as dropdown suggestions

User clicks "India"
  → NavigateTo("/browse?country=India")
  → Browse page OnInitializedAsync()
  → ApiClient.SearchRecipesAsync()
  → GET /api/recipes?country=India
  → RecipesController.Search()
  → RecipeService.SearchAsync()              ← loads user prefs if logged in
  → RecipeRepository.SearchAsync()           ← applies preference filtering
  → returns [RecipeSummaryDto, ...]
  → rendered as recipe cards
```

---

## API Keys Needed

| Key | Where to get |
|-----|-------------|
| `Anthropic:ApiKey` | https://console.anthropic.com → API Keys |
| `GooglePlaces:ApiKey` | https://console.cloud.google.com → Enable "Places API" |

The app works without the Google Places key — the store lookup button will just return empty results. The app works without the Anthropic key only if you remove the AI widget.

---

## Next Steps

Once you're comfortable with the codebase, natural extensions include:

- **Image upload** — store recipe photos in S3/Azure Blob and serve them via CDN
- **Admin panel** — Razor Pages admin UI for adding/editing recipes without touching SQL
- **Ratings & reviews** — add a `RecipeReview` table and aggregate rating on the recipe card
- **Meal planner** — weekly planner page that builds a shopping list from saved recipes
- **Email notifications** — send weekly recipe suggestions based on user preferences
