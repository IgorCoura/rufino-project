import 'package:people_management/people_management.dart';
import 'package:rufino_core/rufino_core.dart';
import '../services/document_dashboard_api_service.dart';

/// Concrete implementation of [DocumentDashboardRepository] backed by
/// [DocumentDashboardApiService].
///
/// All service calls are wrapped in try/catch. [DocumentDashboardException]
/// subtypes are propagated as-is; all other errors are wrapped in
/// [DocumentDashboardNetworkException].
class DocumentDashboardRepositoryImpl implements DocumentDashboardRepository {
  DocumentDashboardRepositoryImpl({
    required this.apiService,
    required this.reporter,
  });

  final DocumentDashboardApiService apiService;
  final ErrorReporter reporter;

  @override
  Future<Result<DashboardSummary>> getSummary(
    String companyId, {
    required int expiringInDays,
    int? employeeStatusId,
    String? employeeName,
    String? documentGroupId,
    String? documentTemplateId,
  }) async {
    try {
      final dto = await apiService.getSummary(
        companyId,
        expiringInDays: expiringInDays,
        employeeStatusId: employeeStatusId,
        employeeName: employeeName,
        documentGroupId: documentGroupId,
        documentTemplateId: documentTemplateId,
      );
      return Result.success(dto.toEntity());
    } on DocumentDashboardException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(DocumentDashboardNetworkException(e), st);
    }
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
    try {
      final response = await apiService.getUnits(
        companyId,
        bucket: bucket.apiValue,
        expiringInDays: expiringInDays,
        employeeStatusId: employeeStatusId,
        employeeName: employeeName,
        documentGroupId: documentGroupId,
        documentTemplateId: documentTemplateId,
        pageSize: pageSize,
        pageNumber: pageNumber,
      );
      return Result.success(DashboardUnitsPage(
        items: response.items.map((m) => m.toEntity()).toList(),
        totalCount: response.totalCount,
      ));
    } on DocumentDashboardException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(DocumentDashboardNetworkException(e), st);
    }
  }
}
