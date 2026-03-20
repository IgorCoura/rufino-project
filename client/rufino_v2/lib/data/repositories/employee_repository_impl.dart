import 'dart:typed_data';

import '../../core/errors/employee_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/employee.dart';
import '../../domain/repositories/employee_repository.dart';
import '../services/employee_api_service.dart';

/// Concrete implementation of [EmployeeRepository] backed by [EmployeeApiService].
///
/// All service calls are wrapped in try/catch. [EmployeeException] subtypes
/// are propagated as-is; all other errors are wrapped in [EmployeeNetworkException].
class EmployeeRepositoryImpl implements EmployeeRepository {
  const EmployeeRepositoryImpl({required this.apiService});

  final EmployeeApiService apiService;

  @override
  Future<Result<List<Employee>>> getEmployees(
    String companyId, {
    String? name,
    String? role,
    int? status,
    int? documentStatus,
    bool ascending = true,
    int pageSize = 15,
    int sizeSkip = 0,
  }) async {
    try {
      final models = await apiService.getEmployees(
        companyId,
        name: name,
        role: role,
        status: status,
        documentStatus: documentStatus,
        sortOrder: ascending ? 0 : 1,
        pageSize: pageSize,
        sizeSkip: sizeSkip,
      );
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<Uint8List?>> getEmployeeImage(
      String companyId, String employeeId) async {
    try {
      final bytes = await apiService.getEmployeeImage(companyId, employeeId);
      return Result.success(bytes);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<String>> createEmployee(
    String companyId, {
    required String name,
    required String roleId,
    required String workplaceId,
  }) async {
    try {
      final id = await apiService.createEmployee(
        companyId,
        name: name,
        roleId: roleId,
        workplaceId: workplaceId,
      );
      return Result.success(id);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }
}
