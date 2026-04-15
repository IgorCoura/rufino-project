import '../../core/result.dart';
import '../entities/address.dart';

/// Contract for resolving a Brazilian CEP into a populated [Address].
abstract class CepRepository {
  /// Returns the [Address] associated with [cep], or a [Result.error]
  /// carrying a [CepException] when the lookup fails.
  Future<Result<Address>> lookupCep(String cep);
}
