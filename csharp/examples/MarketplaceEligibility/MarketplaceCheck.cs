using System.Text.Json;
using VerdictRules;

namespace MarketplaceEligibility;

/// <summary>
/// Marketplace eligibility, implemented with verdict-rules.
/// </summary>
/// <remarks>
/// See fixtures/marketplace_eligibility/README.md for the design and the fixture
/// contract.
/// </remarks>
public static class MarketplaceCheck
{
    /// <summary>
    /// Locate the shared fixture directory by walking up from this assembly's
    /// own location, so it resolves under `dotnet run` and `dotnet test` alike
    /// without either caller hardcoding a depth.
    /// </summary>
    internal static string FindFixtures()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "fixtures", "marketplace_eligibility");
            if (File.Exists(Path.Combine(candidate, "thresholds.json")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"fixtures/marketplace_eligibility not found above {AppContext.BaseDirectory}");
    }

    /// <summary>
    /// The shared policy numbers this example evaluates against.
    /// </summary>
    /// <remarks>
    /// Read from the fixture rather than written as literals here, so the four
    /// ports cannot drift from each other or from the data their suites assert
    /// against -- changing a number in one place changes every port at once.
    /// That is also why these are <c>static readonly</c> rather than
    /// <c>const</c>: a <c>const</c> cannot be read from a file.
    /// </remarks>
    private static readonly Dictionary<string, JsonElement> Thresholds =
        LoadJson(Path.Combine(FindFixtures(), "thresholds.json"));

    public static readonly int PriceFloorCents = Thresholds["price_floor_cents"].GetInt32();

    public static readonly string[] AllowedCategories =
        [.. Thresholds["allowed_categories"].EnumerateArray().Select(e => e.GetString()!)];

    public static readonly int PurchaseLimitCents = Thresholds["purchase_limit_cents"].GetInt32();

    public static readonly int HighValueThresholdCents = Thresholds["high_value_threshold_cents"].GetInt32();

    public static readonly string[] BlockedCountries =
        [.. Thresholds["blocked_countries"].EnumerateArray().Select(e => e.GetString()!)];

    public static readonly int NewSellerThresholdDays = Thresholds["new_seller_threshold_days"].GetInt32();

    private static Task<PredicateOutcome> IsVerifiedIdentity(IdentityFlag context, CancellationToken ct = default) =>
        Task.FromResult(new PredicateOutcome(context.Verified));

    private static ProjectingRule<SellerListingContext, IdentityFlag> SellerIdentityRule() =>
        new(new FunctionRule<IdentityFlag>("is_verified_identity", IsVerifiedIdentity),
            ctx => new IdentityFlag(ctx.SellerVerified));

    private static ProjectingRule<BuyerPurchaseContext, IdentityFlag> BuyerIdentityRule() =>
        new(new FunctionRule<IdentityFlag>("is_verified_identity", IsVerifiedIdentity),
            ctx => new IdentityFlag(ctx.BuyerVerified));

    private static Task<PredicateOutcome> PriceFloorMet(SellerListingContext context, CancellationToken ct = default) =>
        Task.FromResult(new PredicateOutcome(context.ListingPriceCents >= PriceFloorCents));

    private static Task<PredicateOutcome> CategoryAllowed(SellerListingContext context, CancellationToken ct = default) =>
        Task.FromResult(new PredicateOutcome(AllowedCategories.Contains(context.Category)));

    private static Task<PredicateOutcome> SufficientBalance(BuyerPurchaseContext context, CancellationToken ct = default) =>
        Task.FromResult(new PredicateOutcome(context.BuyerBalanceCents >= context.PurchaseAmountCents));

    private static Task<PredicateOutcome> PurchaseLimitNotExceeded(BuyerPurchaseContext context, CancellationToken ct = default) =>
        Task.FromResult(new PredicateOutcome(context.PurchaseAmountCents <= PurchaseLimitCents));

    /// <summary>
    /// Build the typed listing-eligibility composite for one seller.
    /// </summary>
    /// <returns>
    /// A <c>(ListingEligible, SellerVerified)</c> pair -- the full
    /// composite, and the identity sub-rule alone.
    /// </returns>
    public static (AndRule<SellerListingContext> ListingEligible, IRule<SellerListingContext> SellerVerified) BuildSellerCheck()
    {
        var sellerVerified = SellerIdentityRule();
        var listingEligible = new AndRule<SellerListingContext>("listing_eligible",
        [
            sellerVerified,
            new FunctionRule<SellerListingContext>("price_floor_met", PriceFloorMet),
            new FunctionRule<SellerListingContext>("category_allowed", CategoryAllowed),
        ]);
        return (listingEligible, sellerVerified);
    }

    /// <summary>
    /// Build the typed purchase-eligibility composite for one buyer.
    /// </summary>
    /// <returns>The same shape as <see cref="BuildSellerCheck"/>'s own return value.</returns>
    public static (AndRule<BuyerPurchaseContext> PurchaseEligible, IRule<BuyerPurchaseContext> BuyerVerified) BuildBuyerCheck()
    {
        var buyerVerified = BuyerIdentityRule();
        var purchaseEligible = new AndRule<BuyerPurchaseContext>("purchase_eligible",
        [
            buyerVerified,
            new FunctionRule<BuyerPurchaseContext>("sufficient_balance", SufficientBalance),
            new FunctionRule<BuyerPurchaseContext>("purchase_limit_not_exceeded", PurchaseLimitNotExceeded),
        ]);
        return (purchaseEligible, buyerVerified);
    }

    private static Task<PredicateOutcome> HighValueFlag(IReadOnlyDictionary<string, object?> context, CancellationToken ct = default) =>
        Task.FromResult(new PredicateOutcome((int)context["amount_cents"]! > HighValueThresholdCents));

    private static Task<PredicateOutcome> BlockedCountryFlag(IReadOnlyDictionary<string, object?> context, CancellationToken ct = default) =>
        Task.FromResult(new PredicateOutcome(BlockedCountries.Contains((string)context["country"]!)));

    private static Task<PredicateOutcome> NewSellerFlag(IReadOnlyDictionary<string, object?> context, CancellationToken ct = default) =>
        Task.FromResult(new PredicateOutcome((int)context["seller_age_days"]! < NewSellerThresholdDays));

    /// <summary>
    /// Build the dict-context compliance catalog.
    /// </summary>
    /// <remarks>
    /// Unlike the seller/buyer sides, this is deliberately untyped. See
    /// docs/architecture/README.md#generic-context.
    /// </remarks>
    public static RulesEngine BuildComplianceCatalog() => new(
    [
        new FunctionRule("high_value_flag", HighValueFlag),
        new FunctionRule("blocked_country_flag", BlockedCountryFlag),
        new FunctionRule("new_seller_flag", NewSellerFlag),
    ]);

    public static Dictionary<string, JsonElement> LoadJson(string path) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path))!;

    public static SellerListingContext SellerContext(string sellerId, JsonElement row) => new(
        sellerId,
        row.GetProperty("seller_verified").GetBoolean(),
        row.GetProperty("listing_price_cents").GetInt32(),
        row.GetProperty("category").GetString()!);

    public static BuyerPurchaseContext BuyerContext(string buyerId, JsonElement row) => new(
        buyerId,
        row.GetProperty("buyer_verified").GetBoolean(),
        row.GetProperty("purchase_amount_cents").GetInt32(),
        row.GetProperty("buyer_balance_cents").GetInt32());

    public static Dictionary<string, object?> ComplianceContext(JsonElement row) => new()
    {
        ["amount_cents"] = row.GetProperty("amount_cents").GetInt32(),
        ["country"] = row.GetProperty("country").GetString(),
        ["seller_age_days"] = row.GetProperty("seller_age_days").GetInt32(),
    };
}
