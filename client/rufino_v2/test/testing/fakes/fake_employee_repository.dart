import 'dart:typed_data';

import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/repositories/employee_repository.dart';

/// In-memory fake implementation of [EmployeeRepository] for tests.
///
/// All responses are configurable via setters before each test.
class FakeEmployeeRepository implements EmployeeRepository {
  // ─── Configurable return values ───────────────────────────────────────────

  List<Employee> _employees = [];
  bool _shouldFail = false;
  String _createdId = 'new-employee-id';
  Uint8List? _imageBytes;

  void setEmployees(List<Employee> employees) => _employees = employees;

  /// When true every method returns [Result.error] with a generic exception.
  void setShouldFail(bool value) => _shouldFail = value;

  /// The id returned by [createEmployee].
  void setCreatedId(String id) => _createdId = id;

  /// The bytes returned by [getEmployeeImage] (null means no image).
  void setImageBytes(Uint8List? bytes) => _imageBytes = bytes;

  // ─── Captured call arguments (for assertion) ──────────────────────────────

  String? lastCreatedName;
  String? lastCreatedRoleId;
  String? lastCreatedWorkplaceId;
  int? lastGetEmployeesSkip;
  String? lastNameFilter;
  String? lastRoleFilter;
  int getEmployeesCallCount = 0;

  // ─── Implementation ───────────────────────────────────────────────────────

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
    getEmployeesCallCount++;
    lastGetEmployeesSkip = sizeSkip;
    lastNameFilter = name;
    lastRoleFilter = role;
    if (_shouldFail) return Result.error(Exception('getEmployees failed'));
    return Result.success(_employees);
  }

  @override
  Future<Result<Uint8List?>> getEmployeeImage(
      String companyId, String employeeId) async {
    if (_shouldFail) return Result.error(Exception('getEmployeeImage failed'));
    return Result.success(_imageBytes);
  }

  @override
  Future<Result<String>> createEmployee(
    String companyId, {
    required String name,
    required String roleId,
    required String workplaceId,
  }) async {
    if (_shouldFail) return Result.error(Exception('createEmployee failed'));
    lastCreatedName = name;
    lastCreatedRoleId = roleId;
    lastCreatedWorkplaceId = workplaceId;
    return Result.success(_createdId);
  }
}
