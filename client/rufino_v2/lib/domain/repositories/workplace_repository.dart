import '../../core/result.dart';
import '../entities/workplace.dart';

/// Contract for accessing and mutating workplace data.
abstract class WorkplaceRepository {
  /// Returns all workplaces for the given [companyId].
  Future<Result<List<Workplace>>> getWorkplaces(String companyId);

  /// Returns the workplace identified by [workplaceId] within [companyId].
  Future<Result<Workplace>> getWorkplaceById(
      String companyId, String workplaceId);

  /// Creates a new workplace and returns the generated id.
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
  });

  /// Updates an existing workplace identified by [id] and returns its id.
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
  });
}
