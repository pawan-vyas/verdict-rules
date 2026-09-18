/// The narrow context `isVerifiedIdentity` is written against. Shares no
/// fields with either [SellerListingContext] or [BuyerPurchaseContext].
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
/// [SellerListingContext].
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
