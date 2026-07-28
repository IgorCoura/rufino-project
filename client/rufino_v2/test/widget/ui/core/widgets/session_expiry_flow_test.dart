import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:provider/provider.dart';
import 'package:rufino_v2/core/network/session_aware_http_client.dart';
import 'package:rufino_v2/domain/repositories/auth_repository.dart';
import 'package:rufino_v2/ui/core/widgets/session_expired_listener.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/auth_session_notifier.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';

import '../../../../testing/fakes/fake_auth_repository.dart';
import '../../../../testing/fakes/fake_permission_repository.dart';

/// End-to-end wiring of the session-expiry chain, mirroring `app.dart`:
/// a 401 response flips [AuthSessionNotifier] through
/// [SessionAwareHttpClient], the dialog warns the user, and the router
/// guard ([AuthSessionNotifier.redirectFor]) plus the dialog action land
/// on the login screen.
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

  GoRouter buildRouter({required String initialLocation}) {
    return GoRouter(
      navigatorKey: navigatorKey,
      initialLocation: initialLocation,
      redirect: (context, state) =>
          sessionNotifier.redirectFor(state.uri.path),
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
  }

  Widget buildApp({required String initialLocation, bool withListener = true}) {
    router = buildRouter(initialLocation: initialLocation);
    return MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: sessionNotifier),
        ChangeNotifierProvider.value(value: permissionNotifier),
        Provider<AuthRepository>.value(value: authRepository),
      ],
      child: MaterialApp.router(
        routerConfig: router,
        builder: withListener
            ? (context, child) => SessionExpiredListener(
                  router: router,
                  navigatorKey: navigatorKey,
                  child: child ?? const SizedBox.shrink(),
                )
            : null,
      ),
    );
  }

  group('Session expiry end-to-end flow', () {
    testWidgets(
        'a 401 response warns the user and the dialog action lands on the login screen',
        (tester) async {
      final apiClient = SessionAwareHttpClient(
        http_testing.MockClient(
          (request) async =>
              http.Response('{"error": "Authentication failed"}', 401),
        ),
        onSessionInvalid: sessionNotifier.notifySessionExpired,
      );

      await tester.pumpWidget(buildApp(initialLocation: '/home'));
      await tester.pumpAndSettle();

      await apiClient.get(Uri.parse('https://api.test/employees'));
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('session-expired-dialog')), findsOne);

      await tester
          .tap(find.byKey(const ValueKey('session-expired-login-button')));
      await tester.pumpAndSettle();

      expect(find.text('login-screen'), findsOne);
      expect(authRepository.clearLocalSessionCalls, 1);
      expect(sessionNotifier.sessionExpired, isFalse);
    });

    testWidgets(
        'the router guard blocks navigation to a protected route while the session is expired',
        (tester) async {
      // No listener here: on public routes it resets the flag by design,
      // so the guard is exercised in isolation.
      await tester.pumpWidget(
        buildApp(initialLocation: '/login', withListener: false),
      );
      await tester.pumpAndSettle();
      sessionNotifier.notifySessionExpired();

      router.go('/home');
      await tester.pumpAndSettle();

      expect(find.text('login-screen'), findsOne);
      expect(find.text('home-screen'), findsNothing);
    });

    testWidgets(
        'opening the app on a protected route with an expired session lands on the login screen',
        (tester) async {
      sessionNotifier.notifySessionExpired();

      await tester.pumpWidget(buildApp(initialLocation: '/home'));
      await tester.pumpAndSettle();

      expect(find.text('login-screen'), findsOne);
      expect(find.text('home-screen'), findsNothing);
    });
  });
}
