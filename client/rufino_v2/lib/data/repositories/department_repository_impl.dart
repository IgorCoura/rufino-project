import '../../core/errors/department_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/department.dart';
import '../../domain/entities/position.dart';
import '../../domain/entities/remuneration.dart';
import '../../domain/entities/role.dart';
import '../../domain/repositories/department_repository.dart';
import '../models/department_api_model.dart';
import '../services/department_api_service.dart';

/// Concrete implementation of [DepartmentRepository] backed by [DepartmentApiService].
///
/// All service calls are wrapped in try/catch. [DepartmentException] subtypes
/// are propagated as-is; all other errors are wrapped in [DepartmentNetworkException].
class DepartmentRepositoryImpl implements DepartmentRepository {
  const DepartmentRepositoryImpl({required this.apiService});

  final DepartmentApiService apiService;

  // ─── Department ───────────────────────────────────────────────────────────

  @override
  Future<Result<List<Department>>> getDepartments(String companyId) async {
    try {
      final models = await apiService.getDepartments(companyId);
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
  }

  @override
  Future<Result<Department>> getDepartmentById(
      String companyId, String departmentId) async {
    try {
      final model = await apiService.getDepartmentById(companyId, departmentId);
      return Result.success(model.toEntity());
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
  }

  @override
  Future<Result<String>> createDepartment(
    String companyId, {
    required String name,
    required String description,
  }) async {
    try {
      final model = DepartmentApiModel(
        id: '',
        name: name,
        description: description,
        positions: const [],
      );
      final id = await apiService.createDepartment(companyId, model);
      return Result.success(id);
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
  }

  @override
  Future<Result<String>> updateDepartment(
    String companyId, {
    required String id,
    required String name,
    required String description,
  }) async {
    try {
      final model = DepartmentApiModel(
        id: id,
        name: name,
        description: description,
        positions: const [],
      );
      final returnedId = await apiService.updateDepartment(companyId, model);
      return Result.success(returnedId);
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
  }

  // ─── Position ─────────────────────────────────────────────────────────────

  @override
  Future<Result<Position>> getPositionById(
      String companyId, String positionId) async {
    try {
      final model = await apiService.getPositionById(companyId, positionId);
      return Result.success(model.toEntity());
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
  }

  @override
  Future<Result<String>> createPosition(
    String companyId,
    String departmentId, {
    required String name,
    required String description,
    required String cbo,
  }) async {
    try {
      final model = PositionApiModel(
        id: '',
        name: name,
        description: description,
        cbo: cbo,
        roles: const [],
      );
      final id =
          await apiService.createPosition(companyId, departmentId, model);
      return Result.success(id);
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
  }

  @override
  Future<Result<String>> updatePosition(
    String companyId, {
    required String id,
    required String name,
    required String description,
    required String cbo,
  }) async {
    try {
      final model = PositionApiModel(
        id: id,
        name: name,
        description: description,
        cbo: cbo,
        roles: const [],
      );
      final returnedId = await apiService.updatePosition(companyId, model);
      return Result.success(returnedId);
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
  }

  // ─── Role ─────────────────────────────────────────────────────────────────

  @override
  Future<Result<Role>> getRoleById(String companyId, String roleId) async {
    try {
      final model = await apiService.getRoleById(companyId, roleId);
      return Result.success(model.toEntity());
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
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
    try {
      final model = RoleApiModel(
        id: '',
        name: name,
        description: description,
        cbo: cbo,
        remuneration: RemunerationApiModel(
          paymentUnitId: paymentUnitId,
          salaryTypeId: salaryTypeId,
          baseSalaryValue: baseSalaryValue,
          description: remunerationDescription,
        ),
      );
      final id = await apiService.createRole(companyId, positionId, model);
      return Result.success(id);
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
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
    try {
      final model = RoleApiModel(
        id: id,
        name: name,
        description: description,
        cbo: cbo,
        remuneration: RemunerationApiModel(
          paymentUnitId: paymentUnitId,
          salaryTypeId: salaryTypeId,
          baseSalaryValue: baseSalaryValue,
          description: remunerationDescription,
        ),
      );
      final returnedId = await apiService.updateRole(companyId, model);
      return Result.success(returnedId);
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
  }

  // ─── Lookup ───────────────────────────────────────────────────────────────

  @override
  Future<Result<List<PaymentUnit>>> getPaymentUnits(String companyId) async {
    try {
      final models = await apiService.getPaymentUnits(companyId);
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
  }

  @override
  Future<Result<List<SalaryType>>> getSalaryTypes(String companyId) async {
    try {
      final models = await apiService.getSalaryTypes(companyId);
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on DepartmentException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DepartmentNetworkException(e));
    }
  }
}
