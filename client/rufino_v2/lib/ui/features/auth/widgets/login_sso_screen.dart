import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/auth_exception.dart';
import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/login_sso_viewmodel.dart';

/// SSO login screen used when the Authorization Code Flow is enabled.
///
/// Shows a single "Entrar com SSO" button that delegates to
/// [LoginSsoViewModel.submit]; the system browser handles the actual
/// authentication.
class LoginSsoScreen extends StatefulWidget {
  const LoginSsoScreen({super.key, required this.viewModel});

  final LoginSsoViewModel viewModel;

  @override
  State<LoginSsoScreen> createState() => _LoginSsoScreenState();
}

class _LoginSsoScreenState extends State<LoginSsoScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onStatusChanged);
  }

  @override
  void didUpdateWidget(covariant LoginSsoScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      oldWidget.viewModel.removeListener(_onStatusChanged);
      widget.viewModel.addListener(_onStatusChanged);
    }
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onStatusChanged);
    super.dispose();
  }

  void _onStatusChanged() {
    if (!mounted) return;
    switch (widget.viewModel.status) {
      case LoginSsoStatus.success:
        context.go('/');
      case LoginSsoStatus.failure:
        final error = widget.viewModel.lastError;
        final message =
            error != null ? _errorText(error) : 'Falha na autenticação.';
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(
            SnackBar(
              content: Text(message),
              behavior: SnackBarBehavior.floating,
            ),
          );
        widget.viewModel.resetError();
      default:
        break;
    }
  }

  String _errorText(AuthException error) => switch (error) {
        InvalidCredentialsException() => 'Login cancelado ou inválido.',
        SessionExpiredException() => 'Sessão expirada. Tente novamente.',
        NoCredentialsException() => 'Nenhuma credencial encontrada.',
        NetworkAuthException() => 'Falha de conexão. Verifique sua internet.',
      };

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 450),
              child: _Body(viewModel: widget.viewModel),
            ),
          ),
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.viewModel});

  final LoginSsoViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: viewModel,
      builder: (context, _) {
        return Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Rufino',
              style: Theme.of(context).textTheme.displaySmall?.copyWith(
                    color: Theme.of(context).colorScheme.primary,
                    fontWeight: FontWeight.bold,
                  ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(
              'Gestão de Pessoas',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.xxl),
            if (viewModel.isLoading)
              const Center(child: CircularProgressIndicator())
            else
              FilledButton.icon(
                key: const ValueKey('sso_login_button'),
                onPressed: viewModel.submit,
                icon: const Icon(Icons.login),
                label: const Padding(
                  padding: EdgeInsets.symmetric(vertical: AppSpacing.sm),
                  child: Text('Entrar com SSO'),
                ),
              ),
            const SizedBox(height: AppSpacing.md),
            Text(
              'Você será redirecionado ao provedor de identidade para concluir o login.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
              textAlign: TextAlign.center,
            ),
          ],
        );
      },
    );
  }
}
