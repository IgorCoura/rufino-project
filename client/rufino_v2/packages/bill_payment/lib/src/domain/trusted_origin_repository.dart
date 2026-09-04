import 'package:rufino_core/rufino_core.dart';

import 'trusted_origin.dart';

/// Contract for reading and maintaining trusted origins.
abstract class TrustedOriginRepository {
  /// Lists origins, one cursor page at a time.
  Future<Result<TrustedOriginPage>> listOrigins({
    String? cursor,
    int limit = 50,
  });

  /// Returns one origin.
  Future<Result<TrustedOrigin>> getOrigin(String id);

  /// Resolves [sender] respecting the precedence address > e-mail domain >
  /// web domain, or `null` when the origin is unknown — a valid and common
  /// state, not an error.
  Future<Result<TrustedOrigin?>> resolveSender(String sender);

  /// Registers an origin and returns its id. Who decided comes from the
  /// token, on the server.
  Future<Result<String>> registerOrigin({
    required String kind,
    required String value,
    required String decision,
    String? note,
  });

  /// Replaces the decision.
  Future<Result<void>> changeDecision(
    String id, {
    required String decision,
    String? note,
  });

  /// Removes the origin.
  Future<Result<void>> deleteOrigin(String id);
}
