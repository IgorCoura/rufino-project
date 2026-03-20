import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/department.dart';
import 'package:rufino_v2/domain/entities/position.dart';
import 'package:rufino_v2/domain/entities/remuneration.dart';
import 'package:rufino_v2/domain/entities/role.dart';
import 'package:rufino_v2/domain/repositories/department_repository.dart';

/// In-memory fake implementation of [DepartmentRepository] for tests.
///
/// All responses are configurable via setters before each test.
class FakeDepartmentRepository implements DepartmentRepository {
  // ─── Configurable return values ───────────────────────────────────────────

  List<Department> _departments = [];
  Department? _department;
  Position? _position;
  Role? _role;
  List<PaymentUnit> _paymentUnits = [];
  List<SalaryType> _salaryTypes = [];
  bool _shouldFail = false;

  void setDepartments(List<Department> departments) =>
      _departments = departments;
  void setDepartment(Department? d) => _department = d;
  void setPosition(Position? p) => _position = p;
  void setRole(Role? r) => _role = r;
  void setPaymentUnits(List<PaymentUnit> units) => _paymentUnits = units;
  void setSalaryTypes(List<SalaryType> types) => _salaryTypes = types;

  /// When true every method returns [Result.error] with a generic exception.
  void setShouldFail(bool value) => _shouldFail = value;

  // ─── Captured call arguments (for assertion) ──────────────────────────────

  String? lastCreatedDepartmentName;
  String? lastUpdatedDepartmentId;
  String? lastCreatedPositionName;
  String? lastCreatedRoleName;

  // ─── Department ───────────────────────────────────────────────────────────

  @override
  Future<Result<List<Department>>> getDepartments(String companyId) async {
    if (_shouldFail) return Result.error(Exception('getDepartments failed'));
    return Result.success(_departments);
  }

  @override
  Future<Result<Department>> getDepartmentById(
      String companyId, String departmentId) async {
    if (_shouldFail) return Result.error(Exception('getDepartmentById failed'));
    if (_department == null) {
      return Result.error(Exception('Department not found'));
    }
    return Result.success(_department!);
  }

  @override
  Future<Result<String>> createDepartment(
    String companyId, {
    required String name,
    required String description,
  }) async {
    if (_shouldFail) return Result.error(Exception('createDepartment failed'));
    lastCreatedDepartmentName = name;
    return const Result.success('new-dept-id');
  }

  @override
  Future<Result<String>> updateDepartment(
    String companyId, {
    required String id,
    required String name,
    required String description,
  }) async {
    if (_shouldFail) return Result.error(Exception('updateDepartment failed'));
    lastUpdatedDepartmentId = id;
    return Result.success(id);
  }

  // ─── Position ─────────────────────────────────────────────────────────────

  @override
  Future<Result<Position>> getPositionById(
      String companyId, String positionId) async {
    if (_shouldFail) return Result.error(Exception('getPositionById failed'));
    if (_position == null) {
      return Result.error(Exception('Position not found'));
    }
    return Result.success(_position!);
  }

  @override
  Future<Result<String>> createPosition(
    String companyId,
    String departmentId, {
    required String name,
    required String description,
    required String cbo,
  }) async {
    if (_shouldFail) return Result.error(Exception('createPosition failed'));
    lastCreatedPositionName = name;
    return const Result.success('new-pos-id');
  }

  @override
  Future<Result<String>> updatePosition(
    String companyId, {
    required String id,
    required String name,
    required String description,
    required String cbo,
  }) async {
    if (_shouldFail) return Result.error(Exception('updatePosition failed'));
    return Result.success(id);
  }

  // ─── Role ─────────────────────────────────────────────────────────────────

  @override
  Future<Result<Role>> getRoleById(String companyId, String roleId) async {
    if (_shouldFail) return Result.error(Exception('getRoleById failed'));
    if (_role == null) return Result.error(Exception('Role not found'));
    return Result.success(_role!);
  }

  @override
  Future<Result<String>> createRole(
    String companyId,
    String positionId, {
    required String name,
    required String description,
    required String cbo,
    required String paymentUnitId,
    required String salaryTypeId,
    required String baseSalaryValue,
    required String remunerationDescription,
  }) async {
    if (_shouldFail) return Result.error(Exception('createRole failed'));
    lastCreatedRoleName = name;
    return const Result.success('new-role-id');
  }

  @override
  Future<Result<String>> updateRole(
    String companyId, {
    required String id,
    required String name,
    required String description,
    required String cbo,
    required String paymentUnitId,
    required String salaryTypeId,
    required String baseSalaryValue,
    required String remunerationDescription,
  }) async {
    if (_shouldFail) return Result.error(Exception('updateRole failed'));
    return Result.success(id);
  }

  // ─── Lookup ───────────────────────────────────────────────────────────────

  @override
  Future<Result<List<PaymentUnit>>> getPaymentUnits(String companyId) async {
    if (_shouldFail) return Result.error(Exception('getPaymentUnits failed'));
    return Result.success(_paymentUnits);
  }

  @override
  Future<Result<List<SalaryType>>> getSalaryTypes(String companyId) async {
    if (_shouldFail) return Result.error(Exception('getSalaryTypes failed'));
    return Result.success(_salaryTypes);
  }
}
