import '../../data/services/http_exception.dart';

/// Extracts human-readable server error messages from [error].
///
/// Supports [HttpException] directly and any wrapper exception that
/// exposes a `cause` field containing an [HttpException]. Returns an
/// empty list when the error does not carry server messages.
List<String> extractServerMessages(Object error) {
  if (error is HttpException) return error.serverMessages;

  final cause = _unwrapCause(error);
  if (cause is HttpException) return cause.serverMessages;

  return const [];
}

Object? _unwrapCause(Object error) {
  try {
    return (error as dynamic).cause;
  } catch (_) {
    return null;
  }
}
