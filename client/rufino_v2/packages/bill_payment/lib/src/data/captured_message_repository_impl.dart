import 'package:rufino_core/rufino_core.dart';

import '../domain/bill_payment_exception.dart';
import '../domain/captured_message.dart';
import '../domain/captured_message_repository.dart';
import 'captured_message_api_service.dart';

/// Implements [CapturedMessageRepository] over [apiService], reporting at the
/// catch boundary with the module's standard classification.
class CapturedMessageRepositoryImpl implements CapturedMessageRepository {
  /// Creates the repository.
  CapturedMessageRepositoryImpl({
    required this.apiService,
    required this.reporter,
  });

  /// The HTTP client for the capture log endpoints.
  final CapturedMessageApiService apiService;

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

  // O contexto leva só IDs e o nome da operação: remetente e assunto são dado
  // do cliente e não entram nem em diagnóstico.
  @override
  Future<Result<CapturedMessagePage>> listMessages({
    CapturedMessageFilter filter = const CapturedMessageFilter(),
    String? cursor,
    int limit = 50,
  }) =>
      _guard(
        () => apiService.listMessages(
          filter: filter,
          cursor: cursor,
          limit: limit,
        ),
        context: {'op': 'listCapturedMessages'},
      );

  @override
  Future<Result<CaptureSyncStatus>> getSyncStatus() => _guard(
        apiService.getSyncStatus,
        context: {'op': 'getCaptureSyncStatus'},
      );

  @override
  Future<Result<RecaptureOutcome>> recapture(String id) => _guard(
        () => apiService.recapture(id),
        context: {'op': 'recaptureMessage', 'messageId': id},
      );

  @override
  Future<Result<CaptureRetentionPolicy>> getRetentionPolicy() => _guard(
        apiService.getRetentionPolicy,
        context: {'op': 'getCaptureRetentionPolicy'},
      );

  @override
  Future<Result<CaptureRetentionPolicy>> configureRetention({
    required bool isEnabled,
    required int windowDays,
  }) =>
      _guard(
        () => apiService.configureRetention(
          isEnabled: isEnabled,
          windowDays: windowDays,
        ),
        context: {'op': 'configureCaptureRetention', 'windowDays': windowDays},
      );
}
