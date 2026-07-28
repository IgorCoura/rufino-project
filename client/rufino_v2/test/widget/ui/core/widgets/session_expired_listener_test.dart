import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/domain/repositories/auth_repository.dart';
import 'package:rufino_v2/ui/core/widgets/session_expired_listener.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/auth_session_notifier.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';

import '../../../../testing/fakes/fake_auth_repository.dart';
import '../../../../testing/fakes/fake_permission_repository.dart';

void main() {
  late AuthSessionNotifier sessionNotifier;
  late FakeAuthRepository authRepository;
  late PermissionNotifier permissionNotifier;
  late GlobalKey<NavigatorState> navigatorKey;
  late GoRouter router;

  setUp(() {
    sessionNotifier = AuthSessionNotifier();
    authRepository = FakeAuthRepository();
    permissionNotifier =
        PermissionNotifier(permissionRepository: FakePermissionRepository());
    navigatorKey = GlobalKey<NavigatorState>();
  });

  Widget buildApp({required String initialLocation}) {
    router = GoRouter(
      navigatorKey: navigatorKey,
      initialLocation: initialLocation,
      routes: [
        GoRoute(
          path: '/',
          builder: (_, __) => const Scaffold(body: Text('splash-screen')),
        ),
        GoRoute(
          path: '/login',
          builder: (_, __) => const Scaffold(body: Text('login-screen')),
        ),
        GoRoute(
          path: '/home',
          builder: (_, __) => const Scaffold(body: Text('home-screen')),
        ),
      ],
    );

    return MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: sessionNotifier),
        ChangeNotifierProvider.value(value: permissionNotifier),
        Provider<AuthRepository>.value(value: authRepository),
      ],
      child: MaterialApp.router(
        routerConfig: router,
        builder: (context, child) => SessionExpiredListener(
          router: router,
          navigatorKey: navigatorKey,
          child: child ?? const SizedBox.shrink(),
        ),
      ),
    );
  }

  group('SessionExpiredListener', () {
    testWidgets(
        'shows the blocking dialog when the session expires on a protected route',
        (tester) async {
      await tester.pumpWidget(buildApp(initialLocation: '/home'));
      await tester.pumpAndSettle();

      sessionNotifier.notifySessionExpired();
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('session-expired-dialog')), findsOne);
      expect(
        find.text('Sua sessão expirou. Faça login novamente para continuar.'),
        findsOne,
      );
    });

    testWidgets(
        'confirming the dialog clears the local session and navigates to login',
        (tester) async {
      await tester.pumpWidget(buildApp(initialLocation: '/home'));
      await tester.pumpAndSettle();

      sessionNotifier.notifySessionExpired();
      await tester.pumpAndSettle();

      await tester
          .tap(find.byKey(const ValueKey('session-expired-login-button')));
      await tester.pumpAndSettle();

      expect(find.text('login-screen'), findsOne);
      expect(authRepository.clearLocalSessionCalls, 1);
      expect(sessionNotifier.sessionExpired, isFalse);
    });

    testWidgets(
        'does not show the dialog when the session expires on the login screen',
        (tester) async {
      await tester.pumpWidget(buildApp(initialLocation: '/login'));
      await tester.pumpAndSettle();

      sessionNotifier.notifySessionExpired();
      await tester.pumpAndSettle();

      expect(
        find.byKey(const ValueKey('session-expired-dialog')),
        findsNothing,
      );
      expect(sessionNotifier.sessionExpired, isFalse);
    });

    testWidgets(
        'warns again when a new session expires after the first was handled',
        (tester) async {
      await tester.pumpWidget(buildApp(initialLocation: '/home'));
      await tester.pumpAndSettle();

      sessionNotifier.notifySessionExpired();
      await tester.pumpAndSettle();
      await tester
          .tap(find.byKey(const ValueKey('session-expired-login-button')));
      await tester.pumpAndSettle();

      router.go('/home');
      await tester.pumpAndSettle();
      sessionNotifier.notifySessionExpired();
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('session-expired-dialog')), findsOne);
    });
  });
}
