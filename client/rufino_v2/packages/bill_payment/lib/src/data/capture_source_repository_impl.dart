import 'package:rufino_core/rufino_core.dart';

import '../domain/bill_payment_exception.dart';
import '../domain/capture_source.dart';
import '../domain/capture_source_repository.dart';
import 'capture_source_api_service.dart';

/// Implements [CaptureSourceRepository] over [apiService], reporting at the
/// catch boundary with the module's standard classification.
///
/// Context maps carry only ids — never the address, and obviously never the
/// credential.
class CaptureSourceRepositoryImpl implements CaptureSourceRepository {
  /// Creates the repository.
  CaptureSourceRepositoryImpl({
    required this.apiService,
    required this.reporter,
  });

  /// The HTTP client for the capture source endpoints.
  final CaptureSourceApiService apiService;

  /// Where unexpected failures are reported.
  final ErrorReporter reporter;

  Future<Result<T>> _guard<T>(
    Future<T> Function() action, {
    Map<String, dynamic>? context,
  }) async {
    try {
      return Result.success(await action());
    } on HttpException catch (e, st) {
      if (e.statusCode < 500 && e.serverMessages.isNotEmpty) {
        return reporter.failure(
          BillPaymentRuleException(
            e.serverMessages.first,
            code: e.domainErrorId,
          ),
          st,
          context: context,
        );
      }
      return reporter.failure(
        BillPaymentNetworkException(e),
        st,
        context: context,
      );
    } on BillPaymentException catch (e, st) {
      return reporter.failure(e, st, context: context);
    } catch (e, st) {
      return reporter.failure(
        BillPaymentNetworkException(e),
        st,
        context: context,
      );
    }
  }

  @override
  Future<Result<CaptureSourcePage>> listSources({
    String? cursor,
    int limit = 50,
  }) =>
      _guard(
        () => apiService.listSources(cursor: cursor, limit: limit),
        context: {'op': 'listSources'},
      );

  @override
  Future<Result<CaptureSource>> getSource(String id) => _guard(
        () => apiService.getSource(id),
        context: {'op': 'getSource', 'sourceId': id},
      );

  @override
  Future<Result<ConnectOutcome>> connectSource({
    required String displayName,
    required String address,
    required GraphCredentialInput credential,
    String? folderPath,
  }) {
    return _guard(
      () => apiService.connectSource(
        displayName: displayName,
        address: address,
        credential: credential,
        folderPath: folderPath,
      ),
      context: {'op': 'connectSource'},
    );
  }

  @override
  Future<Result<void>> renameSource(String id, String displayName) => _guard(
        () => apiService.renameSource(id, displayName),
        context: {'op': 'renameSource', 'sourceId': id},
      );

  @override
  Future<Result<void>> setActivation(String id, {required bool isEnabled}) =>
      _guard(
        () => apiService.setActivation(id, isEnabled: isEnabled),
        context: {'op': 'setSourceActivation', 'sourceId': id},
      );

  @override
  Future<Result<void>> replaceCredential(
    String id,
    GraphCredentialInput credential,
  ) =>
      _guard(
        () => apiService.replaceCredential(id, credential),
        context: {'op': 'replaceCredential', 'sourceId': id},
      );

  @override
  Future<Result<SyncOutcome>> syncSource(String id) => _guard(
        () => apiService.syncSource(id),
        context: {'op': 'syncSource', 'sourceId': id},
      );

  @override
  Future<Result<void>> addFolder(String id, String? folderPath) => _guard(
        () => apiService.addFolder(id, folderPath),
        context: {'op': 'addFolder', 'sourceId': id},
      );

  @override
  Future<Result<void>> removeFolder(String id, String? folderPath) => _guard(
        () => apiService.removeFolder(id, folderPath),
        context: {'op': 'removeFolder', 'sourceId': id},
      );

  @override
  Future<Result<RescanOutcome>> rescanSource(String id) => _guard(
        () => apiService.rescanSource(id),
        context: {'op': 'rescanSource', 'sourceId': id},
      );

  @override
  Future<Result<void>> disconnectSource(String id) => _guard(
        () => apiService.disconnectSource(id),
        context: {'op': 'disconnectSource', 'sourceId': id},
      );
}
