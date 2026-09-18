using MarketplaceEligibility;

// Fixture data lives at the repo root, shared by every language's own port of
// this example -- see fixtures/marketplace_eligibility/README.md for the contract.
var fixtures = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "fixtures", "marketplace_eligibility");

var sellers = MarketplaceCheck.LoadJson(Path.Combine(fixtures, "sellers.json"));
var buyers = MarketplaceCheck.LoadJson(Path.Combine(fixtures, "buyers.json"));
var events = MarketplaceCheck.LoadJson(Path.Combine(fixtures, "compliance_events.json"));
var catalog = MarketplaceCheck.BuildComplianceCatalog();

Console.WriteLine("=== Marketplace Eligibility -- Demo ===\n");
Console.WriteLine("--- Sellers ---");
foreach (var (sellerId, row) in sellers)
{
    var (listingEligible, _) = MarketplaceCheck.BuildSellerCheck();
    var result = await listingEligible.EvaluateAsync(MarketplaceCheck.SellerContext(sellerId, row));
    Console.WriteLine($"{sellerId,-14}: {(result.Passed ? "ELIGIBLE" : "NOT ELIGIBLE")}");
}

Console.WriteLine("\n--- Buyers ---");
foreach (var (buyerId, row) in buyers)
{
    var (purchaseEligible, _) = MarketplaceCheck.BuildBuyerCheck();
    var result = await purchaseEligible.EvaluateAsync(MarketplaceCheck.BuyerContext(buyerId, row));
    Console.WriteLine($"{buyerId,-14}: {(result.Passed ? "ELIGIBLE" : "NOT ELIGIBLE")}");
}

Console.WriteLine("\n--- Compliance events (one shared, untyped engine) ---");
foreach (var (eventId, row) in events)
{
    var context = MarketplaceCheck.ComplianceContext(row);
    var fired = new List<string>();
    foreach (var name in catalog.RuleNames)
    {
        if ((await catalog.RunNamedAsync(name, context)).Passed)
        {
            fired.Add(name);
        }
    }
    Console.WriteLine($"{eventId,-22}: {(fired.Count > 0 ? $"[{string.Join(", ", fired)}]" : "clean")}");
}
