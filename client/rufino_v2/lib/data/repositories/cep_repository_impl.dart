import '../../core/errors/cep_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/address.dart';
import '../../domain/repositories/cep_repository.dart';
import '../services/cep_api_service.dart';

/// Concrete implementation of [CepRepository] that delegates to
/// [CepApiService] and wraps outcomes in a [Result].
class CepRepositoryImpl implements CepRepository {
  const CepRepositoryImpl({required this.apiService});

  final CepApiService apiService;

  @override
  Future<Result<Address>> lookupCep(String cep) async {
    try {
      final dto = await apiService.lookup(cep);
      return Result.success(dto.toAddress());
    } on CepException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(CepLookupException(e));
    }
  }
}
