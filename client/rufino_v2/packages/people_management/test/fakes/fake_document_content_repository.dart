import 'package:rufino_core/rufino_core.dart';
import 'package:people_management/people_management.dart';
/// In-memory fake implementation of [DocumentContentRepository] for tests.
///
/// Statuses are configured per `documentUnitId`; any unit not configured is
/// reported as up to date.
class FakeDocumentContentRepository implements DocumentContentRepository {
  final Map<String, DocumentContentStatus> _statuses = {};
  bool _shouldFailCheck = false;
  bool _shouldFailRefresh = false;

  /// Marks [documentUnitId] as diverging from the current data.
  void markOutdated(String documentUnitId) {
    _statuses[documentUnitId] = DocumentContentStatus(
      documentUnitId: documentUnitId,
      isOutdated: true,
      checkFailed: false,
    );
  }

  /// Marks [documentUnitId] as inconclusive — outdated but unverifiable.
  void markCheckFailed(String documentUnitId) {
    _statuses[documentUnitId] = DocumentContentStatus(
      documentUnitId: documentUnitId,
      isOutdated: true,
      checkFailed: true,
    );
  }

  /// Makes [checkOutdated] return an error result.
  void setCheckShouldFail(bool value) => _shouldFailCheck = value;

  /// Makes [refresh] return an error result.
  void setRefreshShouldFail(bool value) => _shouldFailRefresh = value;

  /// How many times [checkOutdated] was called.
  int checkCallCount = 0;

  /// How many times [refresh] was called.
  int refreshCallCount = 0;

  /// The items received by the last [checkOutdated] call.
  List<DocumentUnitRefApiModel> lastCheckedItems = const [];

  /// The items received by the last [refresh] call.
  List<DocumentUnitRefApiModel> lastRefreshedItems = const [];

  @override
  Future<Result<List<DocumentContentStatus>>> checkOutdated(
    String companyId,
    List<DocumentUnitRefApiModel> items,
  ) async {
    checkCallCount++;
    lastCheckedItems = items;
    if (_shouldFailCheck) {
      return Result.error(Exception('checkOutdated failed'));
    }
    return Result.success([
      for (final item in items)
        _statuses[item.documentUnitId] ??
            DocumentContentStatus(
              documentUnitId: item.documentUnitId,
              isOutdated: false,
              checkFailed: false,
            ),
    ]);
  }

  @override
  Future<Result<void>> refresh(
    String companyId,
    List<DocumentUnitRefApiModel> items,
  ) async {
    refreshCallCount++;
    lastRefreshedItems = items;
    if (_shouldFailRefresh) {
      return Result.error(Exception('refresh failed'));
    }
    for (final item in items) {
      _statuses.remove(item.documentUnitId);
    }
    return const Result.success(null);
  }
}
