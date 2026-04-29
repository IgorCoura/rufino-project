import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rufino_v2/core/errors/auth_exception.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/login_sso_viewmodel.dart';
import 'package:rufino_v2/ui/features/auth/widgets/login_sso_screen.dart';

import '../../../testing/fakes/fake_auth_repository.dart';

void main() {
  late FakeAuthRepository fakeAuth;
  late LoginSsoViewModel viewModel;

  setUp(() {
    fakeAuth = FakeAuthRepository();
    viewModel = LoginSsoViewModel(authRepository: fakeAuth);
  });

  tearDown(() => viewModel.dispose());

  Widget buildSubject() => MaterialApp.router(
        routerConfig: GoRouter(
          routes: [
            GoRoute(
              path: '/',
              builder: (_, __) => LoginSsoScreen(viewModel: viewModel),
            ),
            GoRoute(
              path: '/home',
              builder: (_, __) => const Scaffold(body: Text('Home')),
            ),
          ],
        ),
      );

  group('LoginSsoScreen', () {
    testWidgets('renders the SSO entry button and explanatory copy',
        (tester) async {
      await tester.pumpWidget(buildSubject());
      await tester.pump();

      expect(find.byKey(const ValueKey('sso_login_button')), findsOneWidget);
      expect(find.text('Entrar com SSO'), findsOneWidget);
      expect(
        find.textContaining('redirecionado ao provedor de identidade'),
        findsOneWidget,
      );
    });

    testWidgets('shows a SnackBar with the localized message on failure',
        (tester) async {
      fakeAuth.setLoginError(const InvalidCredentialsException());
      await tester.pumpWidget(buildSubject());
      await tester.pump();

      await tester.tap(find.byKey(const ValueKey('sso_login_button')));
      await tester.pump();
      await tester.pump();

      expect(find.text('Login cancelado ou inválido.'), findsOneWidget);
    });
  });
}
