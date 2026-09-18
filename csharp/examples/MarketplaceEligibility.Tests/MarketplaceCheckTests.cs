using System.Text.Json;
using VerdictRules;
using Xunit;

namespace MarketplaceEligibility.Tests;

/// <summary>
/// Tests for the marketplace-eligibility example.
/// </summary>
/// <remarks>
/// This project exercises IRule&lt;TContext&gt; end to end: two typed
/// contexts sharing no fields, a rule reused across both via
/// ProjectingRule, and a dict-context catalog coexisting in the same
/// codebase. See docs/samples/marketplace-eligibility/README.md for the
/// design and fixtures/marketplace_eligibility/README.md for the shared
/// contract this suite reproduces.
/// </remarks>
public static class SharedFixture
{
    public static readonly string FixturesDir = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "fixtures", "marketplace_eligibility");

    public static readonly Dictionary<string, JsonElement> Sellers = MarketplaceCheck.LoadJson(Path.Combine(FixturesDir, "sellers.json"));
    public static readonly Dictionary<string, JsonElement> Buyers = MarketplaceCheck.LoadJson(Path.Combine(FixturesDir, "buyers.json"));
    public static readonly Dictionary<string, JsonElement> Events = MarketplaceCheck.LoadJson(Path.Combine(FixturesDir, "compliance_events.json"));
}

/// <summary>Every expectation in sellers.json, asserted -- the typed IRule&lt;SellerListingContext&gt; side.</summary>
public class SellerListingEligibilityTests
{
    public static IEnumerable<object[]> SellerIds() => SharedFixture.Sellers.Keys.Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(SellerIds))]
    public async Task SellerVerifiedMatches(string sellerId)
    {
        var row = SharedFixture.Sellers[sellerId];
        var (_, sellerVerified) = MarketplaceCheck.BuildSellerCheck();
        var result = await sellerVerified.EvaluateAsync(MarketplaceCheck.SellerContext(sellerId, row));
        Assert.True(result.Passed == row.GetProperty("expected").GetProperty("seller_verified").GetBoolean(), sellerId);
    }

    [Theory]
    [MemberData(nameof(SellerIds))]
    public async Task ListingEligibleMatches(string sellerId)
    {
        var row = SharedFixture.Sellers[sellerId];
        var (listingEligible, _) = MarketplaceCheck.BuildSellerCheck();
        var result = await listingEligible.EvaluateAsync(MarketplaceCheck.SellerContext(sellerId, row));
        Assert.True(result.Passed == row.GetProperty("expected").GetProperty("listing_eligible").GetBoolean(), sellerId);
    }
}

/// <summary>Every expectation in buyers.json, asserted -- the typed IRule&lt;BuyerPurchaseContext&gt; side.</summary>
public class BuyerPurchaseEligibilityTests
{
    public static IEnumerable<object[]> BuyerIds() => SharedFixture.Buyers.Keys.Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(BuyerIds))]
    public async Task BuyerVerifiedMatches(string buyerId)
    {
        var row = SharedFixture.Buyers[buyerId];
        var (_, buyerVerified) = MarketplaceCheck.BuildBuyerCheck();
        var result = await buyerVerified.EvaluateAsync(MarketplaceCheck.BuyerContext(buyerId, row));
        Assert.True(result.Passed == row.GetProperty("expected").GetProperty("buyer_verified").GetBoolean(), buyerId);
    }

    [Theory]
    [MemberData(nameof(BuyerIds))]
    public async Task PurchaseEligibleMatches(string buyerId)
    {
        var row = SharedFixture.Buyers[buyerId];
        var (purchaseEligible, _) = MarketplaceCheck.BuildBuyerCheck();
        var result = await purchaseEligible.EvaluateAsync(MarketplaceCheck.BuyerContext(buyerId, row));
        Assert.True(result.Passed == row.GetProperty("expected").GetProperty("purchase_eligible").GetBoolean(), buyerId);
    }
}

/// <summary>The identity check is one instance, projected two ways -- not two copies.</summary>
public class ProjectingRuleReusesOneRuleAcrossContextsTests
{
    [Fact]
    public void BothSidesWrapTheSameNamedRule()
    {
        var (_, sellerVerified) = MarketplaceCheck.BuildSellerCheck();
        var (_, buyerVerified) = MarketplaceCheck.BuildBuyerCheck();
        // Both projections wrap a FunctionRule named "is_verified_identity" --
        // the same rule, reused via a different projection function per side,
        // not two independently-written checks that happen to agree.
        Assert.Equal("is_verified_identity", sellerVerified.Name);
        Assert.Equal("is_verified_identity", buyerVerified.Name);
    }

    [Fact]
    public async Task TheProjectionReadsADifferentFieldPerSide()
    {
        // seller_bob has seller_verified=false -- proving the adapter, not
        // the rule, is what changes between reuse sites.
        var row = SharedFixture.Sellers["seller_bob"];
        var (_, sellerVerified) = MarketplaceCheck.BuildSellerCheck();
        var result = await sellerVerified.EvaluateAsync(MarketplaceCheck.SellerContext("seller_bob", row));
        Assert.False(result.Passed);
    }
}

/// <summary>Every expectation in compliance_events.json, asserted -- the untyped RulesEngine side.</summary>
public class ComplianceCatalogTests
{
    public static IEnumerable<object[]> EventIds() => SharedFixture.Events.Keys.Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(EventIds))]
    public async Task HighValueFlagMatches(string eventId)
    {
        var row = SharedFixture.Events[eventId];
        var catalog = MarketplaceCheck.BuildComplianceCatalog();
        var result = await catalog.RunNamedAsync("high_value_flag", MarketplaceCheck.ComplianceContext(row));
        Assert.True(result.Passed == row.GetProperty("expected").GetProperty("high_value_flag").GetBoolean(), eventId);
    }

    [Theory]
    [MemberData(nameof(EventIds))]
    public async Task BlockedCountryFlagMatches(string eventId)
    {
        var row = SharedFixture.Events[eventId];
        var catalog = MarketplaceCheck.BuildComplianceCatalog();
        var result = await catalog.RunNamedAsync("blocked_country_flag", MarketplaceCheck.ComplianceContext(row));
        Assert.True(result.Passed == row.GetProperty("expected").GetProperty("blocked_country_flag").GetBoolean(), eventId);
    }

    [Theory]
    [MemberData(nameof(EventIds))]
    public async Task NewSellerFlagMatches(string eventId)
    {
        var row = SharedFixture.Events[eventId];
        var catalog = MarketplaceCheck.BuildComplianceCatalog();
        var result = await catalog.RunNamedAsync("new_seller_flag", MarketplaceCheck.ComplianceContext(row));
        Assert.True(result.Passed == row.GetProperty("expected").GetProperty("new_seller_flag").GetBoolean(), eventId);
    }

    [Fact]
    public async Task FlagsAreIndependentNotACompositeVerdict()
    {
        // event_high_value trips exactly one flag -- proving the three
        // checks are looked up independently, not folded into one AND/OR.
        var row = SharedFixture.Events["event_high_value"];
        var catalog = MarketplaceCheck.BuildComplianceCatalog();
        var context = MarketplaceCheck.ComplianceContext(row);
        var fired = new List<string>();
        foreach (var name in catalog.RuleNames)
        {
            if ((await catalog.RunNamedAsync(name, context)).Passed)
            {
                fired.Add(name);
            }
        }
        Assert.Equal(["high_value_flag"], fired);
    }

    [Fact]
    public async Task ANewFlagIsAdditiveNotASharedContextChange()
    {
        // Registering a fourth check needs no change to SellerListingContext,
        // BuyerPurchaseContext, or any existing flag.
        var extended = new RulesEngine(
        [
            new FunctionRule("high_value_flag", (ctx, _) =>
                Task.FromResult(new RuleResult("high_value_flag", (int)ctx["amount_cents"]! > 50_000))),
            new FunctionRule("weekend_flag", (ctx, _) =>
                Task.FromResult(new RuleResult("weekend_flag", (bool)ctx["is_weekend"]!))),
        ]);
        var result = await extended.RunNamedAsync("weekend_flag", new Dictionary<string, object?>
        {
            ["is_weekend"] = true,
            ["amount_cents"] = 0,
        });
        Assert.True(result.Passed);
    }
}
