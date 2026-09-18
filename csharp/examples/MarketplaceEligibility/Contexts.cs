namespace MarketplaceEligibility;

/// <summary>
/// The narrow context <c>IsVerifiedIdentity</c> is written against. Shares
/// no fields with either <see cref="SellerListingContext"/> or
/// <see cref="BuyerPurchaseContext"/>.
/// </summary>
public sealed record IdentityFlag(bool Verified);

/// <summary>What a listing-eligibility check reads.</summary>
public sealed record SellerListingContext(string SellerId, bool SellerVerified, int ListingPriceCents, string Category);

/// <summary>
/// What a purchase-eligibility check reads. Shares no fields with
/// <see cref="SellerListingContext"/>.
/// </summary>
public sealed record BuyerPurchaseContext(string BuyerId, bool BuyerVerified, int PurchaseAmountCents, int BuyerBalanceCents);
