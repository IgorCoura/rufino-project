import 'dart:typed_data';

import '../../core/result.dart';
import '../entities/employee.dart';

/// Contract for accessing and creating employee data.
abstract class EmployeeRepository {
  /// Returns a page of employees for [companyId] matching the given filters.
  ///
  /// [name] filters by employee name (null = no filter).
  /// [role] filters by role name (null = no filter).
  /// [status] filters by [EmployeeStatus] (null = all statuses).
  /// [documentStatus] filters by [DocumentStatus] (null = all statuses).
  /// [ascending] when true results are sorted A→Z; false for Z→A.
  /// [pageSize] is the maximum number of results to return.
  /// [sizeSkip] is the number of records to skip (offset-based pagination).
  Future<Result<List<Employee>>> getEmployees(
    String companyId, {
    String? name,
    String? role,
    int? status,
    int? documentStatus,
    bool ascending = true,
    int pageSize = 15,
    int sizeSkip = 0,
  });

  /// Returns the profile image bytes for [employeeId], or null if none exists.
  Future<Result<Uint8List?>> getEmployeeImage(
      String companyId, String employeeId);

  /// Creates a new employee and returns the generated id.
  ///
  /// [companyId] is the owning company.
  /// [name] is the full name of the employee.
  /// [roleId] is the id of the assigned role.
  /// [workplaceId] is the id of the assigned workplace.
  Future<Result<String>> createEmployee(
    String companyId, {
    required String name,
    required String roleId,
    required String workplaceId,
  });
}
