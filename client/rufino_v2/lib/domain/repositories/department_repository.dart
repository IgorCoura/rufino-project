import '../../core/result.dart';
import '../entities/department.dart';
import '../entities/position.dart';
import '../entities/remuneration.dart';
import '../entities/role.dart';

/// Contract for all department-related data operations.
///
/// Implementations are responsible for fetching and persisting departments,
/// positions, and roles from the remote API and for supplying lookup data such
/// as payment units and salary types needed by forms.
abstract class DepartmentRepository {
  // ─── Department ───────────────────────────────────────────────────────────

  /// Returns all [Department]s for the given [companyId], each fully populated
  /// with its positions and roles.
  Future<Result<List<Department>>> getDepartments(String companyId);

  /// Returns a single [Department] by [departmentId] without its nested positions.
  Future<Result<Department>> getDepartmentById(
      String companyId, String departmentId);

  /// Persists a new department and returns the generated id.
  Future<Result<String>> createDepartment(
    String companyId, {
    required String name,
    required String description,
  });

  /// Updates an existing department and returns its id.
  Future<Result<String>> updateDepartment(
    String companyId, {
    required String id,
    required String name,
    required String description,
  });

  // ─── Position ─────────────────────────────────────────────────────────────

  /// Returns a single [Position] by [positionId] without its nested roles.
  Future<Result<Position>> getPositionById(
      String companyId, String positionId);

  /// Persists a new position under [departmentId] and returns the generated id.
  Future<Result<String>> createPosition(
    String companyId,
    String departmentId, {
    required String name,
    required String description,
    required String cbo,
  });

  /// Updates an existing position and returns its id.
  Future<Result<String>> updatePosition(
    String companyId, {
    required String id,
    required String name,
    required String description,
    required String cbo,
  });

  // ─── Role ─────────────────────────────────────────────────────────────────

  /// Returns a single [Role] by [roleId], fully populated with its remuneration.
  Future<Result<Role>> getRoleById(String companyId, String roleId);

  /// Persists a new role under [positionId] and returns the generated id.
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
  });

  /// Updates an existing role and returns its id.
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
  });

  // ─── Lookup data ──────────────────────────────────────────────────────────

  /// Returns all available [PaymentUnit] options from the API.
  Future<Result<List<PaymentUnit>>> getPaymentUnits(String companyId);

  /// Returns all available [SalaryType] options from the API.
  Future<Result<List<SalaryType>>> getSalaryTypes(String companyId);
}
