import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:nested/nested.dart';
import 'package:provider/provider.dart';

import 'core/config/app_config.dart';
import 'core/storage/secure_storage.dart';
import 'core/theme/app_theme.dart';
import 'core/theme/theme_notifier.dart';
import 'data/repositories/auth_repository_impl.dart';
import 'data/repositories/company_repository_impl.dart';
import 'data/services/auth_api_service.dart';
import 'data/services/company_api_service.dart';
import 'domain/repositories/auth_repository.dart';
import 'domain/repositories/company_repository.dart';
import 'ui/features/auth/viewmodel/login_viewmodel.dart';
import 'ui/features/auth/viewmodel/splash_viewmodel.dart';
import 'ui/features/auth/widgets/login_screen.dart';
import 'ui/features/auth/widgets/splash_screen.dart';
import 'ui/features/company/viewmodel/company_form_viewmodel.dart';
import 'ui/features/company/viewmodel/company_selection_viewmodel.dart';
import 'ui/features/company/widgets/company_form_screen.dart';
import 'ui/features/company/widgets/company_selection_screen.dart';
import 'ui/features/home/viewmodel/home_viewmodel.dart';
import 'ui/features/home/widgets/home_screen.dart';

class App extends StatelessWidget {
  const App({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: _buildProviders(),
      child: _AppRouter(),
    );
  }

  List<SingleChildWidget> _buildProviders() {
    // Infrastructure
    const secureStorage = SecureStorage(FlutterSecureStorage());
    final httpClient = http.Client();

    // Services
    final authApiService = AuthApiService(
      storage: secureStorage,
      authorizationEndpoint: Uri.parse(AppConfig.authorizationEndpoint),
      endSessionEndpoint: Uri.parse(AppConfig.endSessionEndpoint),
      identifier: AppConfig.identifier,
      secret: AppConfig.secret,
    );

    final companyApiService = CompanyApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: authApiService.getAuthorizationHeader,
    );

    // Repositories
    final AuthRepository authRepository = AuthRepositoryImpl(
      authApiService: authApiService,
    );
    final CompanyRepository companyRepository = CompanyRepositoryImpl(
      companyApiService: companyApiService,
      storage: secureStorage,
    );

    return [
      ChangeNotifierProvider(create: (_) => ThemeNotifier()),
      Provider<AuthRepository>.value(value: authRepository),
      Provider<CompanyRepository>.value(value: companyRepository),
    ];
  }
}

class _AppRouter extends StatefulWidget {
  @override
  State<_AppRouter> createState() => _AppRouterState();
}

class _AppRouterState extends State<_AppRouter> {
  late final GoRouter _router;

  @override
  void initState() {
    super.initState();
    _router = GoRouter(
      initialLocation: '/',
      routes: [
        GoRoute(
          path: '/',
          builder: (context, state) => SplashScreen(
            viewModel: SplashViewModel(
              authRepository: context.read<AuthRepository>(),
              companyRepository: context.read<CompanyRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/login',
          builder: (context, state) => LoginScreen(
            viewModel: LoginViewModel(
              authRepository: context.read<AuthRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/company',
          builder: (context, state) => CompanySelectionScreen(
            viewModel: CompanySelectionViewModel(
              authRepository: context.read<AuthRepository>(),
              companyRepository: context.read<CompanyRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/company/create',
          builder: (context, state) => CompanyFormScreen(
            viewModel: CompanyFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/company/edit/:id',
          builder: (context, state) => CompanyFormScreen(
            viewModel: CompanyFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
            ),
            companyId: state.pathParameters['id'],
          ),
        ),
        GoRoute(
          path: '/home',
          builder: (context, state) => HomeScreen(
            viewModel: HomeViewModel(
              authRepository: context.read<AuthRepository>(),
              companyRepository: context.read<CompanyRepository>(),
            ),
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final themeMode = context.watch<ThemeNotifier>().mode;
    return MaterialApp.router(
      title: 'Rufino',
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      themeMode: themeMode,
      routerConfig: _router,
    );
  }
}
