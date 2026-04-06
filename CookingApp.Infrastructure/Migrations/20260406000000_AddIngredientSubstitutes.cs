using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookingApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientSubstitutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert new ingredients — skipped if the name already exists (case-insensitive)
            migrationBuilder.Sql(@"
                INSERT INTO ""Ingredients"" (""Name"", ""Category"")
                SELECT v.name, v.category
                FROM (VALUES
                    ('Allspice',            'spice'),
                    ('Cinnamon',            'spice'),
                    ('Baking Powder',       'leavening'),
                    ('Baking Soda',         'leavening'),
                    ('Butter',              'dairy'),
                    ('Margarine',           'fat'),
                    ('Vegetable Oil',       'fat'),
                    ('Buttermilk',          'dairy'),
                    ('Yogurt',              'dairy'),
                    ('Milk',                'dairy'),
                    ('Chicken Thighs',      'protein'),
                    ('Heavy Cream',         'dairy'),
                    ('Evaporated Milk',     'dairy'),
                    ('Flax Egg',            'other'),
                    ('Garlic Powder',       'spice'),
                    ('Ground Ginger',       'spice'),
                    ('Honey',               'sweetener'),
                    ('Maple Syrup',         'sweetener'),
                    ('Lemon Juice',         'condiment'),
                    ('Lime Juice',          'condiment'),
                    ('Vinegar',             'condiment'),
                    ('Oat Milk',            'dairy'),
                    ('Soy Milk',            'dairy'),
                    ('Shallots',            'vegetable'),
                    ('Leek',                'vegetable'),
                    ('Parmesan Cheese',     'dairy'),
                    ('Pecorino Romano',     'dairy'),
                    ('Ricotta',             'dairy'),
                    ('Cottage Cheese',      'dairy'),
                    ('Sour Cream',          'dairy'),
                    ('Greek Yogurt',        'dairy'),
                    ('Worcestershire Sauce','condiment'),
                    ('White Sugar',         'sweetener'),
                    ('Brown Sugar',         'sweetener'),
                    ('Coconut Oil',         'fat'),
                    ('Cracker Crumbs',      'grain'),
                    ('Applesauce',          'condiment')
                ) AS v(name, category)
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Ingredients"" i WHERE LOWER(i.""Name"") = LOWER(v.name)
                )
            ");

            // Insert substitute relationships, looking up ingredient IDs by name
            migrationBuilder.Sql(@"
                INSERT INTO ""IngredientSubstitutes"" (""OriginalIngredientId"", ""SubstituteIngredientId"", ""ClosenessRank"", ""Explanation"", ""DishImpact"")
                SELECT
                    (SELECT ""Id"" FROM ""Ingredients"" WHERE LOWER(""Name"") = LOWER(v.orig) LIMIT 1),
                    (SELECT ""Id"" FROM ""Ingredients"" WHERE LOWER(""Name"") = LOWER(v.sub)  LIMIT 1),
                    v.rank,
                    v.explanation,
                    v.impact
                FROM (VALUES
                    ('Allspice',        'Cinnamon',             2, 'Use 1/2 tsp cinnamon + 1/4 tsp ginger + 1/4 tsp cloves per 1 tsp allspice', 'Slightly less complex but very close'),
                    ('Baking Powder',   'Baking Soda',          2, 'Use 1/4 tsp baking soda + 1/2 tsp cream of tartar per 1 tsp baking powder',  'Works well in most recipes'),
                    ('Butter',          'Margarine',            2, 'Use same quantity',                                                             'Very similar in baking'),
                    ('Butter',          'Vegetable Oil',        3, 'Use 7/8 cup oil per 1 cup butter',                                             'Less rich flavour, works in most baking'),
                    ('Buttermilk',      'Yogurt',               2, 'Use same quantity',                                                             'Nearly identical in recipes'),
                    ('Buttermilk',      'Milk',                 3, 'Add 1 tablespoon lemon juice or vinegar per cup of milk',                      'Works well after standing 5 minutes'),
                    ('Chicken Breast',  'Chicken Thighs',       2, 'Use same quantity, thighs are juicier',                                        'Richer flavour, slightly more fat, takes slightly longer to cook'),
                    ('Heavy Cream',     'Evaporated Milk',      2, 'Use same quantity',                                                             'Less rich but works in most recipes'),
                    ('Egg',             'Flax Egg',             3, 'Mix 1 tbsp ground flaxseed with 3 tbsp water, let sit 5 mins',                 'Works in baking, not for scrambled eggs'),
                    ('Garlic',          'Garlic Powder',        2, 'Use 1/8 tsp powder per clove',                                                 'Less fresh flavour but works well in cooked dishes'),
                    ('Ginger',          'Ground Ginger',        2, 'Use 1/2 tsp dried per 1 tsp fresh',                                            'Slightly less bright flavour'),
                    ('Honey',           'Maple Syrup',          2, 'Use same quantity',                                                             'Slightly different flavour but works in most recipes'),
                    ('Lemon Juice',     'Lime Juice',           2, 'Use same quantity',                                                             'Slightly more bitter but very similar'),
                    ('Lemon Juice',     'Vinegar',              3, 'Use half the amount',                                                           'More acidic, use sparingly'),
                    ('Milk',            'Oat Milk',             2, 'Use same quantity',                                                             'Slightly sweeter, works in most recipes'),
                    ('Milk',            'Soy Milk',             2, 'Use same quantity',                                                             'Very similar in recipes'),
                    ('Onion',           'Shallots',             2, 'Use same quantity',                                                             'Milder and slightly sweeter'),
                    ('Onion',           'Leek',                 3, 'Use same quantity, white part only',                                           'Milder flavour'),
                    ('Parmesan Cheese', 'Pecorino Romano',      2, 'Use same quantity',                                                             'Slightly saltier and sharper'),
                    ('Ricotta',         'Cottage Cheese',       2, 'Use same quantity, blend for smoother texture',                                 'Slightly less rich'),
                    ('Sour Cream',      'Greek Yogurt',         2, 'Use same quantity',                                                             'Slightly tangier but nearly identical'),
                    ('Soy Sauce',       'Worcestershire Sauce', 3, 'Use half the amount mixed with water',                                          'Different flavour profile but adds umami'),
                    ('White Sugar',     'Brown Sugar',          2, 'Use same quantity',                                                             'Adds slight molasses flavour'),
                    ('Yogurt',          'Sour Cream',           2, 'Use same quantity',                                                             'Slightly richer but very similar'),
                    ('Butter',          'Coconut Oil',          3, 'Use same quantity',                                                             'Adds slight coconut flavour'),
                    ('Breadcrumbs',     'Cracker Crumbs',       2, 'Use same quantity',                                                             'Very similar texture and result'),
                    ('Vegetable Oil',   'Applesauce',           3, 'Use same quantity in baking only',                                              'Lower fat, slightly denser texture')
                ) AS v(orig, sub, rank, explanation, impact)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete the substitute rows added by this migration
            migrationBuilder.Sql(@"
                DELETE FROM ""IngredientSubstitutes""
                WHERE (""OriginalIngredientId"", ""SubstituteIngredientId"") IN (
                    SELECT
                        (SELECT ""Id"" FROM ""Ingredients"" WHERE LOWER(""Name"") = LOWER(v.orig) LIMIT 1),
                        (SELECT ""Id"" FROM ""Ingredients"" WHERE LOWER(""Name"") = LOWER(v.sub)  LIMIT 1)
                    FROM (VALUES
                        ('Allspice',        'Cinnamon'),
                        ('Baking Powder',   'Baking Soda'),
                        ('Butter',          'Margarine'),
                        ('Butter',          'Vegetable Oil'),
                        ('Buttermilk',      'Yogurt'),
                        ('Buttermilk',      'Milk'),
                        ('Chicken Breast',  'Chicken Thighs'),
                        ('Heavy Cream',     'Evaporated Milk'),
                        ('Egg',             'Flax Egg'),
                        ('Garlic',          'Garlic Powder'),
                        ('Ginger',          'Ground Ginger'),
                        ('Honey',           'Maple Syrup'),
                        ('Lemon Juice',     'Lime Juice'),
                        ('Lemon Juice',     'Vinegar'),
                        ('Milk',            'Oat Milk'),
                        ('Milk',            'Soy Milk'),
                        ('Onion',           'Shallots'),
                        ('Onion',           'Leek'),
                        ('Parmesan Cheese', 'Pecorino Romano'),
                        ('Ricotta',         'Cottage Cheese'),
                        ('Sour Cream',      'Greek Yogurt'),
                        ('Soy Sauce',       'Worcestershire Sauce'),
                        ('White Sugar',     'Brown Sugar'),
                        ('Yogurt',          'Sour Cream'),
                        ('Butter',          'Coconut Oil'),
                        ('Breadcrumbs',     'Cracker Crumbs'),
                        ('Vegetable Oil',   'Applesauce')
                    ) AS v(orig, sub)
                )
            ");

            // Delete ingredient rows added by this migration (Id > 20 guards the original seed data)
            migrationBuilder.Sql(@"
                DELETE FROM ""Ingredients""
                WHERE ""Id"" > 20
                AND LOWER(""Name"") IN (
                    'allspice', 'cinnamon', 'baking powder', 'baking soda',
                    'butter', 'margarine', 'vegetable oil', 'buttermilk',
                    'yogurt', 'milk', 'chicken thighs', 'heavy cream',
                    'evaporated milk', 'flax egg', 'garlic powder', 'ground ginger',
                    'honey', 'maple syrup', 'lemon juice', 'lime juice',
                    'vinegar', 'oat milk', 'soy milk', 'shallots', 'leek',
                    'parmesan cheese', 'pecorino romano', 'ricotta', 'cottage cheese',
                    'sour cream', 'greek yogurt', 'worcestershire sauce',
                    'white sugar', 'brown sugar', 'coconut oil', 'cracker crumbs',
                    'applesauce'
                )
            ");
        }
    }
}
