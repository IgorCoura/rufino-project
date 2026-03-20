import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/splash_viewmodel.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key, required this.viewModel});

  final SplashViewModel viewModel;

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onStatusChanged);
    widget.viewModel.initialize();
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onStatusChanged);
    super.dispose();
  }

  void _onStatusChanged() {
    if (!mounted) return;
    switch (widget.viewModel.status) {
      case SplashStatus.authenticated:
        context.go('/home');
      case SplashStatus.unauthenticated:
        context.go('/login');
      case SplashStatus.noCompany:
        context.go('/company');
      case SplashStatus.error:
        context.go('/login');
      case SplashStatus.loading:
        break;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'Rufino',
              style: Theme.of(context).textTheme.displayMedium?.copyWith(
                    color: Theme.of(context).colorScheme.primary,
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: AppSpacing.xxl),
            const CircularProgressIndicator(),
          ],
        ),
      ),
    );
  }
}
