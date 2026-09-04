import 'package:rufino_core/rufino_core.dart';
/// User-facing message shown when the session is no longer valid.
const String sessionExpiredMessage =
    'Sua sessão expirou. Faça login novamente.';

/// User-facing message shown when the user lacks permission (403).
const String accessDeniedMessage =
    'Acesso negado. Você não tem permissão para executar esta ação.';

/// Extracts human-readable server error messages from [error].
///
/// Auth failures produce their canonical messages: [SessionExpiredException]
/// and [NoCredentialsException] yield [sessionExpiredMessage], and
/// [AccessDeniedException] yields [accessDeniedMessage]. Otherwise supports
/// [HttpException] directly and any wrapper exception that exposes a
/// `cause` field containing one of the above. Returns an empty list when
/// the error does not carry server messages.
List<String> extractServerMessages(Object error) {
  final authMessages = _authMessages(error);
  if (authMessages != null) return authMessages;

  if (error is HttpException) return error.serverMessages;

  final cause = _unwrapCause(error);
  if (cause != null) {
    final causeAuthMessages = _authMessages(cause);
    if (causeAuthMessages != null) return causeAuthMessages;
    if (cause is HttpException) return cause.serverMessages;
  }

  return const [];
}

List<String>? _authMessages(Object error) {
  if (error is SessionExpiredException || error is NoCredentialsException) {
    return const [sessionExpiredMessage];
  }
  if (error is AccessDeniedException) return const [accessDeniedMessage];
  return null;
}

Object? _unwrapCause(Object error) {
  try {
    return (error as dynamic).cause;
  } catch (_) {
    return null;
  }
}
