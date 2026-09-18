/// The narrow context `isVerifiedIdentity` is written against. Shares no
/// fields with either [SellerListingContext] or [BuyerPurchaseContext] --
/// the point being demonstrated is that a rule written against this alone
/// can be reused against both, via `ProjectingRule`, without ever seeing
/// either wider context directly.
class IdentityFlag {
  final bool verified;
  const IdentityFlag({required this.verified});
}

/// What a listing-eligibility check reads.
class SellerListingContext {
  final String sellerId;
  final bool sellerVerified;
  final int listingPriceCents;
  final String category;

  const SellerListingContext({
    required this.sellerId,
    required this.sellerVerified,
    required this.listingPriceCents,
    required this.category,
  });
}

/// What a purchase-eligibility check reads. Shares no fields with
/// [SellerListingContext] -- this is deliberate; the only thing the two
/// sides have in common is that both need an identity-verification check,
/// which is exactly what `ProjectingRule` exists to let them share without
/// a common context.
class BuyerPurchaseContext {
  final String buyerId;
  final bool buyerVerified;
  final int purchaseAmountCents;
  final int buyerBalanceCents;

  const BuyerPurchaseContext({
    required this.buyerId,
    required this.buyerVerified,
    required this.purchaseAmountCents,
    required this.buyerBalanceCents,
  });
}
