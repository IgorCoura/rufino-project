import 'package:rufino_v2/core/errors/cep_exception.dart';
import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/repositories/cep_repository.dart';

/// In-memory fake implementation of [CepRepository] for tests.
///
/// Configure behavior via [setAddress], [setNotFound], or [setError] before
/// each test. When no setter is called, [lookupCep] returns a default
/// "not found" result.
class FakeCepRepository implements CepRepository {
  Address? _address;
  Object? _error;
  Duration? _delay;

  /// Captures the CEPs requested during the test.
  final List<String> lookedUpCeps = [];

  /// Makes the next lookup succeed with [address].
  void setAddress(Address address) {
    _address = address;
    _error = null;
  }

  /// Makes the next lookup fail with [CepNotFoundException].
  void setNotFound() {
    _address = null;
    _error = const CepNotFoundException();
  }

  /// Makes the next lookup fail with a generic [CepLookupException].
  void setError([Object? cause]) {
    _address = null;
    _error = CepLookupException(cause ?? 'fake error');
  }

  /// Delays the response by [delay] before completing. Useful for
  /// exercising the loading state.
  void setDelay(Duration delay) => _delay = delay;

  @override
  Future<Result<Address>> lookupCep(String cep) async {
    lookedUpCeps.add(cep);
    if (_delay != null) {
      await Future<void>.delayed(_delay!);
    }
    if (_error != null) return Result.error(_error!);
    if (_address != null) return Result.success(_address!);
    return const Result.error(CepNotFoundException());
  }
}
