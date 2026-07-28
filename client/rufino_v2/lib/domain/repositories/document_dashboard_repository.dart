import '../../core/result.dart';
import '../entities/document_dashboard.dart';

/// Read-only access to the company-wide document dashboard.
abstract class DocumentDashboardRepository {
  /// Fetches the per-bucket unit counts for the company.
  ///
  /// [expiringInDays] is the horizon of the "expiring" bucket, counted in
  /// days from today.
  Future<Result<DashboardSummary>> getSummary(
    String companyId, {
    required int expiringInDays,
    int? employeeStatusId,
    String? employeeName,
    String? documentGroupId,
    String? documentTemplateId,
  });

  /// Fetches the paginated unit list of a dashboard [bucket], ordered by
  /// urgency (closest validity first).
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
  });
}
