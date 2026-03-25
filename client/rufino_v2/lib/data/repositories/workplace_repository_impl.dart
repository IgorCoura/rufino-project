import '../../core/errors/workplace_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/workplace.dart';
import '../../domain/repositories/workplace_repository.dart';
import '../models/workplace_api_model.dart';
import '../services/workplace_api_service.dart';

/// Concrete implementation of [WorkplaceRepository] backed by [WorkplaceApiService].
///
/// All service calls are wrapped in try/catch. [WorkplaceException] subtypes
/// are propagated as-is; all other errors are wrapped in [WorkplaceNetworkException].
class WorkplaceRepositoryImpl implements WorkplaceRepository {
  const WorkplaceRepositoryImpl({required this.apiService});

  final WorkplaceApiService apiService;

  @override
  Future<Result<List<Workplace>>> getWorkplaces(String companyId) async {
    try {
      final models = await apiService.getWorkplaces(companyId);
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on WorkplaceException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(WorkplaceNetworkException(e));
    }
  }

  @override
  Future<Result<Workplace>> getWorkplaceById(
      String companyId, String workplaceId) async {
    try {
      final model = await apiService.getWorkplaceById(companyId, workplaceId);
      return Result.success(model.toEntity());
    } on WorkplaceException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(WorkplaceNetworkException(e));
    }
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
    try {
      final model = WorkplaceApiModel(
        id: '',
        name: name,
        address: AddressApiModel(
          zipCode: zipCode,
          street: street,
          number: number,
          complement: complement,
          neighborhood: neighborhood,
          city: city,
          state: state,
          country: country,
        ),
      );
      final id = await apiService.createWorkplace(companyId, model);
      return Result.success(id);
    } on WorkplaceException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(WorkplaceNetworkException(e));
    }
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
    try {
      final model = WorkplaceApiModel(
        id: id,
        name: name,
        address: AddressApiModel(
          zipCode: zipCode,
          street: street,
          number: number,
          complement: complement,
          neighborhood: neighborhood,
          city: city,
          state: state,
          country: country,
        ),
      );
      final returnedId = await apiService.updateWorkplace(companyId, model);
      return Result.success(returnedId);
    } on WorkplaceException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(WorkplaceNetworkException(e));
    }
  }
}
