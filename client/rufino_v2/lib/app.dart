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
import 'data/repositories/department_repository_impl.dart';
import 'data/repositories/employee_repository_impl.dart';
import 'data/repositories/document_group_repository_impl.dart';
import 'data/repositories/document_template_repository_impl.dart';
import 'data/repositories/require_document_repository_impl.dart';
import 'data/repositories/workplace_repository_impl.dart';
import 'data/services/auth_api_service.dart';
import 'data/services/company_api_service.dart';
import 'data/services/department_api_service.dart';
import 'data/services/document_group_api_service.dart';
import 'data/services/employee_api_service.dart';
import 'data/services/document_template_api_service.dart';
import 'data/services/require_document_api_service.dart';
import 'data/services/workplace_api_service.dart';
import 'domain/repositories/auth_repository.dart';
import 'domain/repositories/company_repository.dart';
import 'domain/repositories/department_repository.dart';
import 'domain/repositories/document_group_repository.dart';
import 'domain/repositories/employee_repository.dart';
import 'domain/repositories/document_template_repository.dart';
import 'domain/repositories/require_document_repository.dart';
import 'domain/repositories/workplace_repository.dart';
import 'ui/features/auth/viewmodel/login_viewmodel.dart';
import 'ui/features/auth/viewmodel/splash_viewmodel.dart';
import 'ui/features/auth/widgets/login_screen.dart';
import 'ui/features/auth/widgets/splash_screen.dart';
import 'ui/features/company/viewmodel/company_form_viewmodel.dart';
import 'ui/features/company/viewmodel/company_selection_viewmodel.dart';
import 'ui/features/company/widgets/company_form_screen.dart';
import 'ui/features/company/widgets/company_selection_screen.dart';
import 'ui/features/department/viewmodel/department_form_viewmodel.dart';
import 'ui/features/document_group/viewmodel/document_group_form_viewmodel.dart';
import 'ui/features/document_group/viewmodel/document_group_list_viewmodel.dart';
import 'ui/features/document_group/widgets/document_group_form_screen.dart';
import 'ui/features/document_group/widgets/document_group_list_screen.dart';
import 'ui/features/document_template/viewmodel/document_template_form_viewmodel.dart';
import 'ui/features/document_template/viewmodel/document_template_list_viewmodel.dart';
import 'ui/features/document_template/widgets/document_template_form_screen.dart';
import 'ui/features/document_template/widgets/document_template_list_screen.dart';
import 'ui/features/department/viewmodel/department_list_viewmodel.dart';
import 'ui/features/department/viewmodel/position_form_viewmodel.dart';
import 'ui/features/department/viewmodel/role_form_viewmodel.dart';
import 'ui/features/department/widgets/department_form_screen.dart';
import 'ui/features/department/widgets/department_list_screen.dart';
import 'ui/features/department/widgets/position_form_screen.dart';
import 'ui/features/department/widgets/role_form_screen.dart';
import 'ui/features/require_document/viewmodel/require_document_form_viewmodel.dart';
import 'ui/features/require_document/viewmodel/require_document_list_viewmodel.dart';
import 'ui/features/require_document/widgets/require_document_form_screen.dart';
import 'ui/features/require_document/widgets/require_document_list_screen.dart';
import 'ui/features/employee/viewmodel/employee_form_viewmodel.dart';
import 'ui/features/employee/viewmodel/employee_list_viewmodel.dart';
import 'ui/features/employee/viewmodel/employee_profile_viewmodel.dart';
import 'ui/features/employee/widgets/employee_form_screen.dart';
import 'ui/features/employee/widgets/employee_list_screen.dart';
import 'ui/features/employee/widgets/employee_profile_screen.dart';
import 'ui/features/home/viewmodel/home_viewmodel.dart';
import 'ui/features/home/widgets/home_screen.dart';
import 'ui/features/workplace/viewmodel/workplace_form_viewmodel.dart';
import 'ui/features/workplace/viewmodel/workplace_list_viewmodel.dart';
import 'ui/features/workplace/widgets/workplace_form_screen.dart';
import 'ui/features/workplace/widgets/workplace_list_screen.dart';

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

    final departmentApiService = DepartmentApiService(
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
    final DepartmentRepository departmentRepository = DepartmentRepositoryImpl(
      apiService: departmentApiService,
    );

    final workplaceApiService = WorkplaceApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: authApiService.getAuthorizationHeader,
    );
    final WorkplaceRepository workplaceRepository = WorkplaceRepositoryImpl(
      apiService: workplaceApiService,
    );

    final documentGroupApiService = DocumentGroupApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: authApiService.getAuthorizationHeader,
    );
    final DocumentGroupRepository documentGroupRepository =
        DocumentGroupRepositoryImpl(apiService: documentGroupApiService);

    final documentTemplateApiService = DocumentTemplateApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: authApiService.getAuthorizationHeader,
    );
    final DocumentTemplateRepository documentTemplateRepository =
        DocumentTemplateRepositoryImpl(apiService: documentTemplateApiService);

    final requireDocumentApiService = RequireDocumentApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: authApiService.getAuthorizationHeader,
    );
    final RequireDocumentRepository requireDocumentRepository =
        RequireDocumentRepositoryImpl(apiService: requireDocumentApiService);

    final employeeApiService = EmployeeApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: authApiService.getAuthorizationHeader,
    );
    final EmployeeRepository employeeRepository = EmployeeRepositoryImpl(
      apiService: employeeApiService,
    );

    return [
      ChangeNotifierProvider(create: (_) => ThemeNotifier()),
      Provider<AuthRepository>.value(value: authRepository),
      Provider<CompanyRepository>.value(value: companyRepository),
      Provider<DepartmentRepository>.value(value: departmentRepository),
      Provider<WorkplaceRepository>.value(value: workplaceRepository),
      Provider<EmployeeRepository>.value(value: employeeRepository),
      Provider<DocumentGroupRepository>.value(
          value: documentGroupRepository),
      Provider<DocumentTemplateRepository>.value(
          value: documentTemplateRepository),
      Provider<RequireDocumentRepository>.value(
          value: requireDocumentRepository),
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

        // ─── Department ───────────────────────────────────────────────────
        GoRoute(
          path: '/department',
          builder: (context, state) => DepartmentListScreen(
            viewModel: DepartmentListViewModel(
              companyRepository: context.read<CompanyRepository>(),
              departmentRepository: context.read<DepartmentRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/department/create',
          builder: (context, state) => DepartmentFormScreen(
            viewModel: DepartmentFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              departmentRepository: context.read<DepartmentRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/department/edit/:id',
          builder: (context, state) => DepartmentFormScreen(
            viewModel: DepartmentFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              departmentRepository: context.read<DepartmentRepository>(),
            ),
            departmentId: state.pathParameters['id'],
          ),
        ),

        // ─── Position ─────────────────────────────────────────────────────
        GoRoute(
          path: '/department/position/create/:departmentId',
          builder: (context, state) => PositionFormScreen(
            viewModel: PositionFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              departmentRepository: context.read<DepartmentRepository>(),
              departmentId: state.pathParameters['departmentId']!,
            ),
          ),
        ),
        GoRoute(
          path: '/department/position/edit/:departmentId/:id',
          builder: (context, state) => PositionFormScreen(
            viewModel: PositionFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              departmentRepository: context.read<DepartmentRepository>(),
              departmentId: state.pathParameters['departmentId']!,
            ),
            positionId: state.pathParameters['id'],
          ),
        ),

        // ─── Role ─────────────────────────────────────────────────────────
        GoRoute(
          path: '/department/role/create/:positionId',
          builder: (context, state) => RoleFormScreen(
            viewModel: RoleFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              departmentRepository: context.read<DepartmentRepository>(),
              positionId: state.pathParameters['positionId']!,
            ),
          ),
        ),
        GoRoute(
          path: '/department/role/edit/:positionId/:id',
          builder: (context, state) => RoleFormScreen(
            viewModel: RoleFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              departmentRepository: context.read<DepartmentRepository>(),
              positionId: state.pathParameters['positionId']!,
            ),
            roleId: state.pathParameters['id'],
          ),
        ),

        // ─── Document Group ─────────────────────────────────────────────
        GoRoute(
          path: '/document-group',
          builder: (context, state) => DocumentGroupListScreen(
            viewModel: DocumentGroupListViewModel(
              companyRepository: context.read<CompanyRepository>(),
              documentGroupRepository:
                  context.read<DocumentGroupRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/document-group/create',
          builder: (context, state) => DocumentGroupFormScreen(
            viewModel: DocumentGroupFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              documentGroupRepository:
                  context.read<DocumentGroupRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/document-group/edit/:id',
          builder: (context, state) => DocumentGroupFormScreen(
            viewModel: DocumentGroupFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              documentGroupRepository:
                  context.read<DocumentGroupRepository>(),
            ),
            groupId: state.pathParameters['id'],
          ),
        ),

        // ─── Document Template ────────────────────────────────────────────
        GoRoute(
          path: '/document-template',
          builder: (context, state) => DocumentTemplateListScreen(
            viewModel: DocumentTemplateListViewModel(
              companyRepository: context.read<CompanyRepository>(),
              documentTemplateRepository:
                  context.read<DocumentTemplateRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/document-template/create',
          builder: (context, state) => DocumentTemplateFormScreen(
            viewModel: DocumentTemplateFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              documentTemplateRepository:
                  context.read<DocumentTemplateRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/document-template/edit/:id',
          builder: (context, state) => DocumentTemplateFormScreen(
            viewModel: DocumentTemplateFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              documentTemplateRepository:
                  context.read<DocumentTemplateRepository>(),
            ),
            templateId: state.pathParameters['id'],
          ),
        ),

        // ─── Require Document ────────────────────────────────────────────
        GoRoute(
          path: '/require-document',
          builder: (context, state) => RequireDocumentListScreen(
            viewModel: RequireDocumentListViewModel(
              companyRepository: context.read<CompanyRepository>(),
              requireDocumentRepository:
                  context.read<RequireDocumentRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/require-document/create',
          builder: (context, state) => RequireDocumentFormScreen(
            viewModel: RequireDocumentFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              requireDocumentRepository:
                  context.read<RequireDocumentRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/require-document/edit/:id',
          builder: (context, state) => RequireDocumentFormScreen(
            viewModel: RequireDocumentFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              requireDocumentRepository:
                  context.read<RequireDocumentRepository>(),
            ),
            requireDocumentId: state.pathParameters['id'],
          ),
        ),

        // ─── Employee ─────────────────────────────────────────────────────
        GoRoute(
          path: '/employee',
          builder: (context, state) => EmployeeListScreen(
            viewModel: EmployeeListViewModel(
              companyRepository: context.read<CompanyRepository>(),
              employeeRepository: context.read<EmployeeRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/employee/create',
          builder: (context, state) => EmployeeFormScreen(
            viewModel: EmployeeFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              departmentRepository: context.read<DepartmentRepository>(),
              workplaceRepository: context.read<WorkplaceRepository>(),
              employeeRepository: context.read<EmployeeRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/employee/:id',
          builder: (context, state) => EmployeeProfileScreen(
            employeeId: state.pathParameters['id']!,
            viewModel: EmployeeProfileViewModel(
              companyRepository: context.read<CompanyRepository>(),
              employeeRepository: context.read<EmployeeRepository>(),
              departmentRepository: context.read<DepartmentRepository>(),
              workplaceRepository: context.read<WorkplaceRepository>(),
            ),
          ),
        ),

        // ─── Workplace ────────────────────────────────────────────────────
        GoRoute(
          path: '/workplace',
          builder: (context, state) => WorkplaceListScreen(
            viewModel: WorkplaceListViewModel(
              companyRepository: context.read<CompanyRepository>(),
              workplaceRepository: context.read<WorkplaceRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/workplace/create',
          builder: (context, state) => WorkplaceFormScreen(
            viewModel: WorkplaceFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              workplaceRepository: context.read<WorkplaceRepository>(),
            ),
          ),
        ),
        GoRoute(
          path: '/workplace/edit/:id',
          builder: (context, state) => WorkplaceFormScreen(
            viewModel: WorkplaceFormViewModel(
              companyRepository: context.read<CompanyRepository>(),
              workplaceRepository: context.read<WorkplaceRepository>(),
            ),
            workplaceId: state.pathParameters['id'],
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final themeMode = context.watch<ThemeNotifier>().mode;
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      title: 'Rufino',
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      themeMode: themeMode,
      routerConfig: _router,
    );
  }
}
