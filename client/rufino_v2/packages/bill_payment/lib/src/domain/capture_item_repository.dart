import 'package:rufino_core/rufino_core.dart';

import 'capture_item.dart';

/// What claiming an item returned.
class ClaimOutcome {
  /// Creates the outcome record.
  const ClaimOutcome({
    required this.id,
    required this.billId,
    required this.status,
  });

  /// The item's id.
  final String id;

  /// The bill the claim created.
  final String billId;

  /// The item's new status.
  final String status;
}

/// Contract for reading and acting on the quarantine queue.
abstract class CaptureItemRepository {
  /// Lists items, one cursor page at a time, optionally filtered by
  /// [status] (server-side, case-insensitive; an unknown value returns the
  /// whole list).
  Future<Result<CaptureItemPage>> listItems({
    String? status,
    String? cursor,
    int limit = 50,
  });

  /// Returns one item.
  Future<Result<CaptureItem>> getItem(String id);

  /// Sends the item back through the extraction cascade. Spends the vision
  /// extractor's quota — that is why it has its own scope.
  Future<Result<void>> reprocessItem(String id);

  /// Claims an unrouted item as this tenant's bill.
  Future<Result<ClaimOutcome>> claimItem(String id);
}
