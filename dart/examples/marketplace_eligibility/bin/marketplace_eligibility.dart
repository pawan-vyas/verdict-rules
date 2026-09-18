import 'dart:io';

import 'package:marketplace_eligibility/marketplace_eligibility.dart';

Future<void> main() async {
  // Fixture data lives at the repo root, shared by every language's own port
  // of this example -- see fixtures/marketplace_eligibility/README.md for
  // the contract.
  final scriptDir = File(Platform.script.toFilePath()).parent.path;
  final fixtures =
      Directory('$scriptDir/../../../../fixtures/marketplace_eligibility')
          .absolute
          .path;

  final sellers = loadSellers('$fixtures/sellers.json');
  final buyers = loadBuyers('$fixtures/buyers.json');
  final events = loadComplianceEvents('$fixtures/compliance_events.json');
  final catalog = buildComplianceCatalog();

  print('=== Marketplace Eligibility -- Demo ===\n');
  print('--- Sellers ---');
  for (final entry in sellers.entries) {
    final row = entry.value as Map<String, Object?>;
    final (listingEligible, _) = buildSellerCheck();
    final result =
        await listingEligible.evaluate(sellerContext(entry.key, row));
    print(
        '${entry.key.padRight(14)}: ${result.passed ? 'ELIGIBLE' : 'NOT ELIGIBLE'}');
  }

  print('\n--- Buyers ---');
  for (final entry in buyers.entries) {
    final row = entry.value as Map<String, Object?>;
    final (purchaseEligible, _) = buildBuyerCheck();
    final result =
        await purchaseEligible.evaluate(buyerContext(entry.key, row));
    print(
        '${entry.key.padRight(14)}: ${result.passed ? 'ELIGIBLE' : 'NOT ELIGIBLE'}');
  }

  print('\n--- Compliance events (one shared, untyped engine) ---');
  for (final entry in events.entries) {
    final row = entry.value as Map<String, Object?>;
    final fired = <String>[];
    for (final name in catalog.ruleNames) {
      if ((await catalog.runNamed(name, row)).passed) fired.add(name);
    }
    print('${entry.key.padRight(22)}: ${fired.isNotEmpty ? fired : 'clean'}');
  }
}
