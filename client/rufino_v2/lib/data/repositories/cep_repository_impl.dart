import '../../core/errors/cep_exception.dart';
import '../../core/monitoring/error_reporter.dart';
import '../../core/result.dart';
import '../../domain/entities/address.dart';
import '../../domain/repositories/cep_repository.dart';
import '../services/cep_api_service.dart';

/// Concrete implementation of [CepRepository] that delegates to
/// [CepApiService] and wraps outcomes in a [Result].
class CepRepositoryImpl implements CepRepository {
  CepRepositoryImpl({required this.apiService, required this.reporter});

  final CepApiService apiService;
  final ErrorReporter reporter;

  @override
  Future<Result<Address>> lookupCep(String cep) async {
    try {
      final dto = await apiService.lookup(cep);
      return Result.success(dto.toAddress());
    } on CepException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(CepLookupException(e), st);
    }
  }
}
