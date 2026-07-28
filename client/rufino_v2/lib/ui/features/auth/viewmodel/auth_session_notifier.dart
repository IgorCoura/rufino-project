import 'package:flutter/foundation.dart';

/// App-wide session state, flipped to expired when any layer detects that
/// the user's session is no longer valid — a 401 from the backend or an
/// unrecoverable token refresh.
///
/// The UI reacts through `SessionExpiredListener`, which warns the user
/// once and sends them back to the login screen.
class AuthSessionNotifier extends ChangeNotifier {
  bool _sessionExpired = false;

  /// Whether the current session has been flagged as expired.
  bool get sessionExpired => _sessionExpired;

  /// Flags the session as expired.
  ///
  /// Repeated calls while the flag is set are collapsed, so concurrent
  /// failing requests produce a single warning to the user.
  void notifySessionExpired() {
    if (_sessionExpired) return;
    _sessionExpired = true;
    notifyListeners();
  }

  /// Clears the expired flag after the user acknowledged the warning and
  /// was sent to the login screen.
  void reset() {
    if (!_sessionExpired) return;
    _sessionExpired = false;
    notifyListeners();
  }

  /// go_router guard: while the session is flagged expired, any navigation
  /// to a protected route is sent to the login screen.
  ///
  /// Returns `/login` for protected [path]s when expired, or null to allow
  /// the navigation. The splash (`/`) and login routes stay reachable so
  /// the boot and re-login flows are never blocked.
  String? redirectFor(String path) {
    if (!_sessionExpired) return null;
    if (path == '/' || path == '/login') return null;
    return '/login';
  }
}
