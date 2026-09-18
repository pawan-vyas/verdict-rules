using System.Text.Json;
using VerdictRules;

namespace MarketplaceEligibility;

/// <summary>
/// Marketplace eligibility, implemented with verdict-rules.
/// </summary>
/// <remarks>
/// See docs/samples/marketplace-eligibility/README.md for the design and
/// fixtures/marketplace_eligibility/README.md for the fixture contract.
/// </remarks>
public static class MarketplaceCheck
{
    public const int PriceFloorCents = 100;
    public static readonly string[] AllowedCategories = ["books", "electronics", "home"];
    public const int PurchaseLimitCents = 100_000;
    public const int HighValueThresholdCents = 50_000;
    public static readonly string[] BlockedCountries = ["ir", "nk"];
    public const int NewSellerThresholdDays = 30;

    private static Task<RuleResult> IsVerifiedIdentity(IdentityFlag context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("is_verified_identity", context.Verified));

    private static ProjectingRule<SellerListingContext, IdentityFlag> SellerIdentityRule() =>
        new(new FunctionRule<IdentityFlag>("is_verified_identity", IsVerifiedIdentity),
            ctx => new IdentityFlag(ctx.SellerVerified));

    private static ProjectingRule<BuyerPurchaseContext, IdentityFlag> BuyerIdentityRule() =>
        new(new FunctionRule<IdentityFlag>("is_verified_identity", IsVerifiedIdentity),
            ctx => new IdentityFlag(ctx.BuyerVerified));

    private static Task<RuleResult> PriceFloorMet(SellerListingContext context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("price_floor_met", context.ListingPriceCents >= PriceFloorCents));

    private static Task<RuleResult> CategoryAllowed(SellerListingContext context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("category_allowed", AllowedCategories.Contains(context.Category)));

    private static Task<RuleResult> SufficientBalance(BuyerPurchaseContext context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("sufficient_balance", context.BuyerBalanceCents >= context.PurchaseAmountCents));

    private static Task<RuleResult> PurchaseLimitNotExceeded(BuyerPurchaseContext context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("purchase_limit_not_exceeded", context.PurchaseAmountCents <= PurchaseLimitCents));

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

    private static Task<RuleResult> HighValueFlag(IReadOnlyDictionary<string, object?> context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("high_value_flag", (int)context["amount_cents"]! > HighValueThresholdCents));

    private static Task<RuleResult> BlockedCountryFlag(IReadOnlyDictionary<string, object?> context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("blocked_country_flag", BlockedCountries.Contains((string)context["country"]!)));

    private static Task<RuleResult> NewSellerFlag(IReadOnlyDictionary<string, object?> context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("new_seller_flag", (int)context["seller_age_days"]! < NewSellerThresholdDays));

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
