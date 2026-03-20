import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/auth_exception.dart';
import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/login_viewmodel.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, required this.viewModel});

  final LoginViewModel viewModel;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  late final TextEditingController _usernameController;
  late final TextEditingController _passwordController;

  @override
  void initState() {
    super.initState();
    _usernameController = TextEditingController();
    _passwordController = TextEditingController();
    widget.viewModel.addListener(_onStatusChanged);
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    widget.viewModel.removeListener(_onStatusChanged);
    super.dispose();
  }

  void _onStatusChanged() {
    if (!mounted) return;
    switch (widget.viewModel.status) {
      case LoginStatus.success:
        context.go('/');
      case LoginStatus.failure:
        final error = widget.viewModel.lastError;
        final message = error != null ? _errorText(error) : 'Falha na autenticação.';
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
        InvalidCredentialsException() => 'Usuário ou senha incorretos.',
        SessionExpiredException() => 'Sessão expirada. Faça login novamente.',
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
              child: _LoginForm(
                viewModel: widget.viewModel,
                usernameController: _usernameController,
                passwordController: _passwordController,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _LoginForm extends StatelessWidget {
  const _LoginForm({
    required this.viewModel,
    required this.usernameController,
    required this.passwordController,
  });

  final LoginViewModel viewModel;
  final TextEditingController usernameController;
  final TextEditingController passwordController;

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
            TextField(
              key: const ValueKey('username_field'),
              controller: usernameController,
              decoration: const InputDecoration(
                labelText: 'Usuário',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.person_outline),
              ),
              textInputAction: TextInputAction.next,
              onChanged: viewModel.onUsernameChanged,
            ),
            const SizedBox(height: AppSpacing.md),
            _PasswordField(
              controller: passwordController,
              onChanged: viewModel.onPasswordChanged,
              onSubmitted: (_) => viewModel.submit(),
            ),
            const SizedBox(height: AppSpacing.lg),
            _LoginButton(viewModel: viewModel),
          ],
        );
      },
    );
  }
}

class _PasswordField extends StatefulWidget {
  const _PasswordField({
    required this.controller,
    required this.onChanged,
    required this.onSubmitted,
  });

  final TextEditingController controller;
  final ValueChanged<String> onChanged;
  final ValueChanged<String> onSubmitted;

  @override
  State<_PasswordField> createState() => _PasswordFieldState();
}

class _PasswordFieldState extends State<_PasswordField> {
  bool _obscure = true;

  @override
  Widget build(BuildContext context) {
    return TextField(
      key: const ValueKey('password_field'),
      controller: widget.controller,
      obscureText: _obscure,
      decoration: InputDecoration(
        labelText: 'Senha',
        border: const OutlineInputBorder(),
        prefixIcon: const Icon(Icons.lock_outline),
        suffixIcon: IconButton(
          icon: Icon(_obscure ? Icons.visibility_off_outlined : Icons.visibility_outlined),
          onPressed: () => setState(() => _obscure = !_obscure),
          tooltip: _obscure ? 'Mostrar senha' : 'Ocultar senha',
        ),
      ),
      textInputAction: TextInputAction.done,
      onChanged: widget.onChanged,
      onSubmitted: widget.onSubmitted,
    );
  }
}

class _LoginButton extends StatelessWidget {
  const _LoginButton({required this.viewModel});

  final LoginViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    if (viewModel.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    return FilledButton(
      key: const ValueKey('login_button'),
      onPressed: viewModel.submit,
      child: const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Text('Entrar'),
      ),
    );
  }
}
