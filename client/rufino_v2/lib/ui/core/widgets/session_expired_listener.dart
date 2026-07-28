import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../domain/repositories/auth_repository.dart';
import '../../features/auth/viewmodel/auth_session_notifier.dart';
import '../../features/auth/viewmodel/permission_notifier.dart';

/// Watches [AuthSessionNotifier] and, when the session expires, shows a
/// blocking dialog telling the user to log in again.
///
/// Confirming the dialog clears the locally stored credentials and cached
/// permissions, resets the notifier and navigates to the login screen. The
/// warning is skipped when the user is already on a public route (splash
/// or login), where announcing an expired session adds nothing.
class SessionExpiredListener extends StatefulWidget {
  const SessionExpiredListener({
    super.key,
    required this.router,
    required this.navigatorKey,
    required this.child,
  });

  final GoRouter router;
  final GlobalKey<NavigatorState> navigatorKey;
  final Widget child;

  @override
  State<SessionExpiredListener> createState() => _SessionExpiredListenerState();
}

class _SessionExpiredListenerState extends State<SessionExpiredListener> {
  late final AuthSessionNotifier _sessionNotifier;
  bool _dialogVisible = false;

  @override
  void initState() {
    super.initState();
    _sessionNotifier = context.read<AuthSessionNotifier>();
    _sessionNotifier.addListener(_onSessionChanged);
  }

  @override
  void dispose() {
    _sessionNotifier.removeListener(_onSessionChanged);
    super.dispose();
  }

  void _onSessionChanged() {
    if (!_sessionNotifier.sessionExpired || _dialogVisible) return;

    final location =
        widget.router.routerDelegate.currentConfiguration.uri.path;
    if (location == '/' || location == '/login') {
      _sessionNotifier.reset();
      return;
    }

    final dialogContext = widget.navigatorKey.currentContext;
    if (dialogContext == null) return;

    _dialogVisible = true;
    showDialog<void>(
      context: dialogContext,
      barrierDismissible: false,
      builder: (_) => const _SessionExpiredDialog(),
    ).then((_) => _redirectToLogin());
  }

  Future<void> _redirectToLogin() async {
    _dialogVisible = false;
    final authRepository = context.read<AuthRepository>();
    final permissionNotifier = context.read<PermissionNotifier>();

    await authRepository.clearLocalSession();
    await permissionNotifier.clear();
    _sessionNotifier.reset();
    widget.router.go('/login');
  }

  @override
  Widget build(BuildContext context) => widget.child;
}

class _SessionExpiredDialog extends StatelessWidget {
  const _SessionExpiredDialog();

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      key: const ValueKey('session-expired-dialog'),
      title: const Text('Sessão expirada'),
      content: const Text(
        'Sua sessão expirou. Faça login novamente para continuar.',
      ),
      actions: [
        FilledButton(
          key: const ValueKey('session-expired-login-button'),
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Fazer login'),
        ),
      ],
    );
  }
}
