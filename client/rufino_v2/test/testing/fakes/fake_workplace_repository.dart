import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/workplace.dart';
import 'package:rufino_v2/domain/repositories/workplace_repository.dart';

/// In-memory fake implementation of [WorkplaceRepository] for tests.
///
/// All responses are configurable via setters before each test.
class FakeWorkplaceRepository implements WorkplaceRepository {
  // ─── Configurable return values ───────────────────────────────────────────

  List<Workplace> _workplaces = [];
  Workplace? _workplace;
  bool _shouldFail = false;

  void setWorkplaces(List<Workplace> workplaces) => _workplaces = workplaces;
  void setWorkplace(Workplace? w) => _workplace = w;

  /// When true every method returns [Result.error] with a generic exception.
  void setShouldFail(bool value) => _shouldFail = value;

  // ─── Captured call arguments (for assertion) ──────────────────────────────

  String? lastCreatedWorkplaceName;
  String? lastUpdatedWorkplaceId;

  // ─── Implementation ───────────────────────────────────────────────────────

  @override
  Future<Result<List<Workplace>>> getWorkplaces(String companyId) async {
    if (_shouldFail) return Result.error(Exception('getWorkplaces failed'));
    return Result.success(_workplaces);
  }

  @override
  Future<Result<Workplace>> getWorkplaceById(
      String companyId, String workplaceId) async {
    if (_shouldFail) return Result.error(Exception('getWorkplaceById failed'));
    if (_workplace == null) {
      return Result.error(Exception('Workplace not found'));
    }
    return Result.success(_workplace!);
  }

  @override
  Future<Result<String>> createWorkplace(
    String companyId, {
    required String name,
    required String zipCode,
    required String street,
    required String number,
    required String complement,
    required String neighborhood,
    required String city,
    required String state,
    required String country,
  }) async {
    if (_shouldFail) return Result.error(Exception('createWorkplace failed'));
    lastCreatedWorkplaceName = name;
    return const Result.success('new-workplace-id');
  }

  @override
  Future<Result<String>> updateWorkplace(
    String companyId, {
    required String id,
    required String name,
    required String zipCode,
    required String street,
    required String number,
    required String complement,
    required String neighborhood,
    required String city,
    required String state,
    required String country,
  }) async {
    if (_shouldFail) return Result.error(Exception('updateWorkplace failed'));
    lastUpdatedWorkplaceId = id;
    return Result.success(id);
  }
}
