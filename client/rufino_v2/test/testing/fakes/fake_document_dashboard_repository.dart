import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/document_dashboard.dart';
import 'package:rufino_v2/domain/repositories/document_dashboard_repository.dart';

/// In-memory fake implementation of [DocumentDashboardRepository] for tests.
///
/// All responses are configurable via setters, and the last received
/// arguments are recorded for assertions.
class FakeDocumentDashboardRepository implements DocumentDashboardRepository {
  DashboardSummary _summary = DashboardSummary.empty;
  DashboardUnitsPage _page = const DashboardUnitsPage(items: [], totalCount: 0);
  bool _shouldFail = false;

  /// Sets the summary returned by [getSummary].
  void setSummary(DashboardSummary summary) => _summary = summary;

  /// Sets the page returned by [getUnits].
  void setUnitsPage(DashboardUnitsPage page) => _page = page;

  /// Makes every call return an error result.
  void setShouldFail(bool value) => _shouldFail = value;

  /// How many times [getSummary] was called.
  int summaryCallCount = 0;

  /// How many times [getUnits] was called.
  int unitsCallCount = 0;

  /// The horizon received by the last [getSummary] / [getUnits] call.
  int? lastExpiringInDays;

  /// The bucket received by the last [getUnits] call.
  DashboardBucket? lastBucket;

  /// The employee status filter received by the last call.
  int? lastEmployeeStatusId;

  /// The employee name filter received by the last call.
  String? lastEmployeeName;

  /// The group filter received by the last call.
  String? lastDocumentGroupId;

  /// The template filter received by the last call.
  String? lastDocumentTemplateId;

  /// The page number received by the last [getUnits] call.
  int? lastPageNumber;

  /// The page size received by the last [getUnits] call.
  int? lastPageSize;

  @override
  Future<Result<DashboardSummary>> getSummary(
    String companyId, {
    required int expiringInDays,
    int? employeeStatusId,
    String? employeeName,
    String? documentGroupId,
    String? documentTemplateId,
  }) async {
    summaryCallCount++;
    lastExpiringInDays = expiringInDays;
    lastEmployeeStatusId = employeeStatusId;
    lastEmployeeName = employeeName;
    lastDocumentGroupId = documentGroupId;
    lastDocumentTemplateId = documentTemplateId;
    if (_shouldFail) {
      return Result.error(Exception('getSummary failed'));
    }
    return Result.success(_summary);
  }

  @override
  Future<Result<DashboardUnitsPage>> getUnits(
    String companyId, {
    required DashboardBucket bucket,
    required int expiringInDays,
    int? employeeStatusId,
    String? employeeName,
    String? documentGroupId,
    String? documentTemplateId,
    int pageSize = 50,
    int pageNumber = 1,
  }) async {
    unitsCallCount++;
    lastBucket = bucket;
    lastExpiringInDays = expiringInDays;
    lastEmployeeStatusId = employeeStatusId;
    lastEmployeeName = employeeName;
    lastDocumentGroupId = documentGroupId;
    lastDocumentTemplateId = documentTemplateId;
    lastPageSize = pageSize;
    lastPageNumber = pageNumber;
    if (_shouldFail) {
      return Result.error(Exception('getUnits failed'));
    }
    return Result.success(_page);
  }
}
