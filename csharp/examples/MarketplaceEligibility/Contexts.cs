namespace MarketplaceEligibility;

/// <summary>
/// The narrow context <c>IsVerifiedIdentity</c> is written against. Shares
/// no fields with either <see cref="SellerListingContext"/> or
/// <see cref="BuyerPurchaseContext"/> -- the point being demonstrated is
/// that a rule written against this alone can be reused against both, via
/// <see cref="ProjectingRule{TOuter, TInner}"/>, without ever seeing either
/// wider context directly.
/// </summary>
public sealed record IdentityFlag(bool Verified);

/// <summary>What a listing-eligibility check reads.</summary>
public sealed record SellerListingContext(string SellerId, bool SellerVerified, int ListingPriceCents, string Category);

/// <summary>
/// What a purchase-eligibility check reads. Shares no fields with
/// <see cref="SellerListingContext"/> -- this is deliberate; the only
/// thing the two sides have in common is that both need an
/// identity-verification check, which is exactly what
/// <see cref="ProjectingRule{TOuter, TInner}"/> exists to let them share
/// without a common context.
/// </summary>
public sealed record BuyerPurchaseContext(string BuyerId, bool BuyerVerified, int PurchaseAmountCents, int BuyerBalanceCents);
