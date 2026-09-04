import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:tenant_management/tenant_management.dart';

import 'package:rufino_core/rufino_core.dart';
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
  void didUpdateWidget(covariant SplashScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      oldWidget.viewModel.removeListener(_onStatusChanged);
      widget.viewModel.addListener(_onStatusChanged);
      widget.viewModel.initialize();
    }
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onStatusChanged);
    super.dispose();
  }

  void _onStatusChanged() {
    if (!mounted) return;
    if (widget.viewModel.status != SplashStatus.decided) return;

    switch (widget.viewModel.destination) {
      case SplashDestination.home:
        context.go('/home');
      case SplashDestination.login:
        context.go('/login');
      case SplashDestination.selectTenant:
        context.go(TenantRoutes.select);
      case null:
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
