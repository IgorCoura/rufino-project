import 'package:file_picker/file_picker.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:flutter/services.dart';
import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:nested/nested.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:bill_payment/bill_payment.dart';
import 'package:tenant_management/tenant_management.dart';

import 'core/config/app_config.dart';
import 'core/tenant/tenant_session_bridge.dart';
import 'package:rufino_core/rufino_core.dart';
import 'data/repositories/auth_code_repository_impl.dart';
import 'data/repositories/auth_repository_impl.dart';
import 'data/services/auth_code_api_service.dart';
import 'data/services/oauth_login_strategy.dart';
import 'data/services/oauth_login_strategy_factory.dart';
import 'data/services/pending_web_redirect_result.dart';
import 'data/repositories/company_repository_impl.dart';
import 'data/repositories/department_repository_impl.dart';
import 'data/repositories/employee_repository_impl.dart';
import 'data/repositories/document_group_repository_impl.dart';
import 'data/repositories/document_template_repository_impl.dart';
import 'data/repositories/require_document_repository_impl.dart';
import 'data/repositories/batch_document_repository_impl.dart';
import 'data/repositories/batch_download_repository_impl.dart';
import 'data/repositories/document_dashboard_repository_impl.dart';
import 'data/repositories/cep_repository_impl.dart';
import 'data/repositories/document_scanner_repository_impl.dart';
import 'core/utils/document_scanner_service.dart';
import 'data/repositories/workplace_repository_impl.dart';
import 'data/services/auth_api_service.dart';
import 'data/services/company_api_service.dart';
import 'data/services/department_api_service.dart';
import 'data/services/document_group_api_service.dart';
import 'data/services/employee_api_service.dart';
import 'data/services/document_template_api_service.dart';
import 'data/services/require_document_api_service.dart';
import 'data/services/batch_document_api_service.dart';
import 'data/services/batch_download_api_service.dart';
import 'data/services/document_dashboard_api_service.dart';
import 'data/services/cep_api_service.dart';
import 'data/services/file_save_service.dart';
import 'data/services/spreadsheet_service.dart';
import 'data/services/workplace_api_service.dart';
import 'domain/repositories/auth_repository.dart';
import 'domain/repositories/company_repository.dart';
import 'domain/repositories/department_repository.dart';
import 'domain/repositories/document_group_repository.dart';
import 'domain/repositories/employee_repository.dart';
import 'domain/repositories/document_template_repository.dart';
import 'domain/repositories/require_document_repository.dart';
import 'domain/repositories/batch_document_repository.dart';
import 'domain/repositories/batch_download_repository.dart';
import 'domain/repositories/document_dashboard_repository.dart';
import 'domain/repositories/cep_repository.dart';
import 'domain/repositories/workplace_repository.dart';
import 'ui/core/widgets/session_expired_listener.dart';
import 'ui/features/auth/viewmodel/auth_session_notifier.dart';
import 'ui/features/auth/viewmodel/login_sso_viewmodel.dart';
import 'ui/features/auth/viewmodel/login_viewmodel.dart';
import 'ui/features/auth/viewmodel/splash_viewmodel.dart';
import 'ui/features/auth/widgets/login_screen.dart';
import 'ui/features/auth/widgets/login_sso_screen.dart';
import 'ui/features/auth/widgets/splash_screen.dart';
import 'ui/features/company/viewmodel/company_form_viewmodel.dart';
import 'ui/features/company/widgets/company_form_screen.dart';
import 'ui/features/department/viewmodel/department_form_viewmodel.dart';
import 'ui/features/document_group/viewmodel/document_group_form_viewmodel.dart';
import 'ui/features/document_group/viewmodel/document_group_with_templates_viewmodel.dart';
import 'ui/features/document_group/widgets/document_group_form_screen.dart';
import 'ui/features/document_group/widgets/document_group_with_templates_screen.dart';
import 'ui/features/document_template/viewmodel/document_template_form_viewmodel.dart';
import 'ui/features/document_template/widgets/document_template_form_screen.dart';
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
import 'ui/features/employee/viewmodel/employee_profile_viewmodel.dart';
import 'ui/features/employee/widgets/employee_form_screen.dart';
import 'ui/features/employee/widgets/employee_list_screen.dart';
import 'ui/features/employee/widgets/employee_profile_screen.dart';
import 'ui/features/batch_document/viewmodel/batch_document_viewmodel.dart';
import 'ui/features/batch_document/widgets/batch_document_screen.dart';
import 'ui/features/batch_download/viewmodel/batch_download_viewmodel.dart';
import 'ui/features/batch_download/widgets/batch_download_screen.dart';
import 'ui/features/document_dashboard/widgets/document_dashboard_screen.dart';
import 'ui/features/debug/widgets/debug_screen.dart';
import 'ui/features/home/viewmodel/home_viewmodel.dart';
import 'ui/features/home/widgets/home_screen.dart';
import 'ui/features/workplace/viewmodel/workplace_form_viewmodel.dart';
import 'ui/features/workplace/viewmodel/workplace_list_viewmodel.dart';
import 'ui/features/workplace/widgets/workplace_form_screen.dart';
import 'ui/features/workplace/widgets/workplace_list_screen.dart';

class App extends StatelessWidget {
  const App({
    super.key,
    required this.prefs,
    required this.errorReporter,
    this.pendingWebRedirect,
  });

  /// The already-initialized [SharedPreferences] instance, created in `main()`.
  final SharedPreferences prefs;

  /// Provider-agnostic error monitoring sink. Initialized in `main()` and
  /// forwarded down the widget tree via `Provider<ErrorReporter>`.
  final ErrorReporter errorReporter;

  /// Result of a pending web Authorization Code redirect. Only ever set
  /// on Web when [AppConfig.useAuthorizationCodeFlow] is true.
  final PendingWebRedirectResult? pendingWebRedirect;

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
    final authSessionNotifier = AuthSessionNotifier();
    final httpClient = errorReporter.wrapHttpClient(
      SessionAwareHttpClient(
        http.Client(),
        onSessionInvalid: authSessionNotifier.notifySessionExpired,
      ),
    );

    // Auth — pick the active flow at compile time.
    final authApiService = AppConfig.useAuthorizationCodeFlow
        ? null
        : AuthApiService(
            storage: secureStorage,
            authorizationEndpoint: Uri.parse(AppConfig.authorizationEndpoint),
            endSessionEndpoint: Uri.parse(AppConfig.endSessionEndpoint),
            identifier: AppConfig.identifier,
            secret: AppConfig.secret,
            httpClient: httpClient,
          );

    AuthCodeApiService? authCodeApiService;
    if (AppConfig.useAuthorizationCodeFlow) {
      final OAuthLoginStrategy strategy = createOAuthLoginStrategy(
        identifier: AppConfig.identifier,
        secret: AppConfig.secret,
        authorizationEndpoint:
            Uri.parse(AppConfig.authCodeAuthorizationEndpoint),
        tokenEndpoint: Uri.parse(AppConfig.authCodeTokenEndpoint),
        scopes: const ['openid', 'profile', 'email', 'offline_access'],
      );
      authCodeApiService = AuthCodeApiService(
        storage: secureStorage,
        strategy: strategy,
        tokenEndpoint: Uri.parse(AppConfig.authCodeTokenEndpoint),
        endSessionEndpoint: Uri.parse(AppConfig.endSessionEndpoint),
        identifier: AppConfig.identifier,
        secret: AppConfig.secret,
        httpClient: httpClient,
      );

      // Web only: a redirect that brought the user back to the app
      // already produced credentials in main(). Persist them now.
      final pending = pendingWebRedirect;
      if (pending != null && pending.credentials != null) {
        authCodeApiService.primeCredentials(pending.credentials!);
      }
    }

    // Auth failures inside the token callbacks (expired refresh, missing
    // credentials) must raise the app-wide session flag before propagating.
    Future<T> flagSessionLoss<T>(Future<T> Function() action) async {
      try {
        return await action();
      } on SessionExpiredException {
        authSessionNotifier.notifySessionExpired();
        rethrow;
      } on NoCredentialsException {
        authSessionNotifier.notifySessionExpired();
        rethrow;
      }
    }

    final getAccessToken = AppConfig.useAuthorizationCodeFlow
        ? () => flagSessionLoss(() async =>
            (await authCodeApiService!.getCredentials()).accessToken)
        : () => flagSessionLoss(
            () async => (await authApiService!.getCredentials()).accessToken);

    final getAuthHeader = AppConfig.useAuthorizationCodeFlow
        ? () => flagSessionLoss(authCodeApiService!.getAuthorizationHeader)
        : () => flagSessionLoss(authApiService!.getAuthorizationHeader);

    final tokenEndpoint = Uri.parse(
      AppConfig.useDirectAccessGrants
          ? AppConfig.authorizationEndpoint
          : AppConfig.authCodeTokenEndpoint,
    );

    final permissionApiService = PermissionApiService(
      client: httpClient,
      tokenEndpoint: tokenEndpoint,
      getAccessToken: getAccessToken,
      audience: AppConfig.peopleManagementAudience,
    );

    // Segunda audiência: o back-office de tenants vive noutro resource
    // server, e quem não é operador da plataforma recebe 403 aqui — que o
    // cliente UMA traduz como "nenhuma permissão", não como erro.
    final tenantPermissionApiService = PermissionApiService(
      client: httpClient,
      tokenEndpoint: tokenEndpoint,
      getAccessToken: getAccessToken,
      audience: AppConfig.tenantManagementAudience,
    );

    final companyApiService = CompanyApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
    );

    final departmentApiService = DepartmentApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
    );

    // Repositories
    final permissionCacheService = PermissionCacheService(prefs: prefs);
    final PermissionRepository permissionRepository =
        PermissionRepositoryImpl(
      permissionApiService: permissionApiService,
      permissionCacheService: permissionCacheService,
      reporter: errorReporter,
    );
    final permissionNotifier = PermissionNotifier(
      permissionRepository: permissionRepository,
    );

    // Chave de cache própria: duas audiências dividindo uma só se
    // sobrescreveriam, e qual venceria dependeria da ordem de chegada.
    final tenantPermissionRepository = PermissionRepositoryImpl(
      permissionApiService: tenantPermissionApiService,
      permissionCacheService: PermissionCacheService(
        prefs: prefs,
        cacheKey: 'cached_permissions_tenant_management',
      ),
      reporter: errorReporter,
    );
    final tenantPermissionNotifier = TenantPermissionNotifier(
      permissionRepository: tenantPermissionRepository,
    );

    // Terceira audiência: o BillPayment é outro resource server, com cache
    // próprio — duas audiências numa chave só se sobrescreveriam.
    final billPaymentPermissionNotifier = BillPaymentPermissionNotifier(
      permissionRepository: PermissionRepositoryImpl(
        permissionApiService: PermissionApiService(
          client: httpClient,
          tokenEndpoint: tokenEndpoint,
          getAccessToken: getAccessToken,
          audience: AppConfig.billPaymentAudience,
        ),
        permissionCacheService: PermissionCacheService(
          prefs: prefs,
          cacheKey: 'cached_permissions_bill_payment',
        ),
        reporter: errorReporter,
      ),
    );

    // Reload permissions automatically when the access token is refreshed —
    // every audience, or one of them silently goes stale.
    void reloadAllPermissions() {
      permissionNotifier.loadPermissions();
      tenantPermissionNotifier.loadPermissions();
      billPaymentPermissionNotifier.loadPermissions();
    }

    if (authApiService != null) {
      authApiService.onTokenRefreshed = reloadAllPermissions;
    }
    if (authCodeApiService != null) {
      authCodeApiService.onTokenRefreshed = reloadAllPermissions;
    }

    final AuthRepository authRepository = AppConfig.useAuthorizationCodeFlow
        ? AuthCodeRepositoryImpl(
            authCodeApiService: authCodeApiService!,
            reporter: errorReporter,
          )
        : AuthRepositoryImpl(
            authApiService: authApiService!,
            reporter: errorReporter,
          );
    final CompanyRepository companyRepository = CompanyRepositoryImpl(
      companyApiService: companyApiService,
      storage: secureStorage,
      reporter: errorReporter,
    );
    final DepartmentRepository departmentRepository = DepartmentRepositoryImpl(
      apiService: departmentApiService,
      reporter: errorReporter,
    );

    final workplaceApiService = WorkplaceApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
    );
    final WorkplaceRepository workplaceRepository = WorkplaceRepositoryImpl(
      apiService: workplaceApiService,
      reporter: errorReporter,
    );

    final documentGroupApiService = DocumentGroupApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
    );
    final DocumentGroupRepository documentGroupRepository =
        DocumentGroupRepositoryImpl(
      apiService: documentGroupApiService,
      reporter: errorReporter,
    );

    final documentTemplateApiService = DocumentTemplateApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
    );
    final DocumentTemplateRepository documentTemplateRepository =
        DocumentTemplateRepositoryImpl(
      apiService: documentTemplateApiService,
      reporter: errorReporter,
    );

    final requireDocumentApiService = RequireDocumentApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
    );
    final RequireDocumentRepository requireDocumentRepository =
        RequireDocumentRepositoryImpl(
      apiService: requireDocumentApiService,
      reporter: errorReporter,
    );

    final employeeApiService = EmployeeApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
    );
    final EmployeeRepository employeeRepository = EmployeeRepositoryImpl(
      apiService: employeeApiService,
      reporter: errorReporter,
    );

    final batchDocumentApiService = BatchDocumentApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
    );
    final BatchDocumentRepository batchDocumentRepository =
        BatchDocumentRepositoryImpl(
      apiService: batchDocumentApiService,
      reporter: errorReporter,
    );

    final cepApiService = CepApiService(client: httpClient);
    final CepRepository cepRepository = CepRepositoryImpl(
      apiService: cepApiService,
      reporter: errorReporter,
    );

    final batchDownloadApiService = BatchDownloadApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
    );
    final BatchDownloadRepository batchDownloadRepository =
        BatchDownloadRepositoryImpl(
      apiService: batchDownloadApiService,
      reporter: errorReporter,
    );

    final documentDashboardApiService = DocumentDashboardApiService(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
    );
    final DocumentDashboardRepository documentDashboardRepository =
        DocumentDashboardRepositoryImpl(
      apiService: documentDashboardApiService,
      reporter: errorReporter,
    );

    // Tenant: a identidade do cliente da plataforma. O contexto é um só e
    // vive em `rufino_core`, para que todo produto o leia sem depender do
    // pacote que desenha a tela de seleção.
    final tenantContextNotifier = TenantContextNotifier(
      storage: secureStorage,
    );
    final TenantRepository tenantRepository = TenantRepositoryImpl(
      apiService: TenantApiService(
        client: httpClient,
        baseUrl: AppConfig.tenantManagementUrl,
        getAuthHeader: getAuthHeader,
      ),
      reporter: errorReporter,
    );
    final tenantSessionBridge = TenantSessionBridge(
      companyRepository: companyRepository,
      permissionNotifier: permissionNotifier,
      tenantPermissionNotifier: tenantPermissionNotifier,
      billPaymentPermissionNotifier: billPaymentPermissionNotifier,
      errorReporter: errorReporter,
    );

    // BillPayment: os serviços do produto. Toda rota do BC carrega o tenant
    // corrente, lido do contexto único.
    String getBillPaymentTenantId() {
      final tenantId = tenantContextNotifier.tenantId;
      if (tenantId == null) {
        // As rotas do módulo vivem atrás da seleção de tenant; chegar aqui
        // sem contexto é bug de navegação, não estado esperado.
        throw StateError('Nenhum cliente selecionado.');
      }
      return tenantId;
    }

    final BillRepository billRepository = BillRepositoryImpl(
      apiService: BillApiService(
        client: httpClient,
        baseUrl: AppConfig.billPaymentUrl,
        getAuthHeader: getAuthHeader,
        getTenantId: getBillPaymentTenantId,
      ),
      reporter: errorReporter,
    );
    final CaptureItemRepository captureItemRepository =
        CaptureItemRepositoryImpl(
      apiService: CaptureItemApiService(
        client: httpClient,
        baseUrl: AppConfig.billPaymentUrl,
        getAuthHeader: getAuthHeader,
        getTenantId: getBillPaymentTenantId,
      ),
      reporter: errorReporter,
    );
    final CapturedMessageRepository capturedMessageRepository =
        CapturedMessageRepositoryImpl(
      apiService: CapturedMessageApiService(
        client: httpClient,
        baseUrl: AppConfig.billPaymentUrl,
        getAuthHeader: getAuthHeader,
        getTenantId: getBillPaymentTenantId,
      ),
      reporter: errorReporter,
    );
    final CaptureSourceRepository captureSourceRepository =
        CaptureSourceRepositoryImpl(
      apiService: CaptureSourceApiService(
        client: httpClient,
        baseUrl: AppConfig.billPaymentUrl,
        getAuthHeader: getAuthHeader,
        getTenantId: getBillPaymentTenantId,
      ),
      reporter: errorReporter,
    );
    final PayeeRepository payeeRepository = PayeeRepositoryImpl(
      apiService: PayeeApiService(
        client: httpClient,
        baseUrl: AppConfig.billPaymentUrl,
        getAuthHeader: getAuthHeader,
        getTenantId: getBillPaymentTenantId,
      ),
      reporter: errorReporter,
    );
    final PayerProfileRepository payerProfileRepository =
        PayerProfileRepositoryImpl(
      apiService: PayerProfileApiService(
        client: httpClient,
        baseUrl: AppConfig.billPaymentUrl,
        getAuthHeader: getAuthHeader,
        getTenantId: getBillPaymentTenantId,
      ),
      reporter: errorReporter,
    );
    final TrustedOriginRepository trustedOriginRepository =
        TrustedOriginRepositoryImpl(
      apiService: TrustedOriginApiService(
        client: httpClient,
        baseUrl: AppConfig.billPaymentUrl,
        getAuthHeader: getAuthHeader,
        getTenantId: getBillPaymentTenantId,
      ),
      reporter: errorReporter,
    );
    final ExpectationRepository expectationRepository =
        ExpectationRepositoryImpl(
      apiService: ExpectationApiService(
        client: httpClient,
        baseUrl: AppConfig.billPaymentUrl,
        getAuthHeader: getAuthHeader,
        getTenantId: getBillPaymentTenantId,
      ),
      reporter: errorReporter,
    );

    // Spreadsheet export — stateless, safe to share across the app.
    final spreadsheetService = SpreadsheetService();
    final fileSaveService = FileSaveService();

    return [
      Provider<ErrorReporter>.value(value: errorReporter),
      ChangeNotifierProvider(create: (_) => ThemeNotifier()),
      ChangeNotifierProvider.value(value: permissionNotifier),
      ChangeNotifierProvider.value(value: tenantPermissionNotifier),
      ChangeNotifierProvider.value(value: billPaymentPermissionNotifier),
      ChangeNotifierProvider.value(value: tenantContextNotifier),
      ChangeNotifierProvider.value(value: tenantSessionBridge),
      Provider<TenantRepository>.value(value: tenantRepository),
      // Consulta de CEP compartilhada: o cadastro de tenant exige endereço,
      // e a chamada é a mesma que o cadastro de funcionário já fazia.
      Provider<CepLookupService>.value(
        value: CepLookupService(client: httpClient),
      ),
      ChangeNotifierProvider.value(value: authSessionNotifier),
      Provider<AuthRepository>.value(value: authRepository),
      Provider<PermissionRepository>.value(value: permissionRepository),
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
      Provider<BatchDocumentRepository>.value(
          value: batchDocumentRepository),
      Provider<BatchDownloadRepository>.value(
          value: batchDownloadRepository),
      Provider<DocumentDashboardRepository>.value(
          value: documentDashboardRepository),
      Provider<CepRepository>.value(value: cepRepository),
      Provider<SpreadsheetService>.value(value: spreadsheetService),
      Provider<FileSaveService>.value(value: fileSaveService),
      Provider<BillRepository>.value(value: billRepository),
      Provider<CaptureItemRepository>.value(value: captureItemRepository),
      Provider<CapturedMessageRepository>.value(
        value: capturedMessageRepository,
      ),
      Provider<CaptureSourceRepository>.value(
          value: captureSourceRepository),
      Provider<PayeeRepository>.value(value: payeeRepository),
      Provider<PayerProfileRepository>.value(value: payerProfileRepository),
      Provider<TrustedOriginRepository>.value(
          value: trustedOriginRepository),
      Provider<ExpectationRepository>.value(value: expectationRepository),
    ];
  }
}

class _AppRouter extends StatefulWidget {
  @override
  State<_AppRouter> createState() => _AppRouterState();
}

class _AppRouterState extends State<_AppRouter> {
  late final GoRouter _router;
  final _rootNavigatorKey = GlobalKey<NavigatorState>();

  @override
  void initState() {
    super.initState();
    _router = GoRouter(
      navigatorKey: _rootNavigatorKey,
      initialLocation: '/',
      observers: [context.read<ErrorReporter>().navigatorObserver],
      redirect: (context, state) =>
          context.read<AuthSessionNotifier>().redirectFor(state.uri.path),
      routes: [
        GoRoute(
          path: '/',
          builder: (context, state) => SplashScreen(
            viewModel: SplashViewModel(
              authRepository: context.read<AuthRepository>(),
              tenantRepository: context.read<TenantRepository>(),
              tenantContext: context.read<TenantContextNotifier>(),
              tenantSessionBridge: context.read<TenantSessionBridge>(),
              permissionNotifier: context.read<PermissionNotifier>(),
              tenantPermissionNotifier:
                  context.read<TenantPermissionNotifier>(),
              billPaymentPermissionNotifier:
                  context.read<BillPaymentPermissionNotifier>(),
              errorReporter: context.read<ErrorReporter>(),
            ),
          ),
        ),
        GoRoute(
          path: '/login',
          builder: (context, state) {
            if (AppConfig.useAuthorizationCodeFlow) {
              return LoginSsoScreen(
                viewModel: LoginSsoViewModel(
                  authRepository: context.read<AuthRepository>(),
                ),
              );
            }
            return LoginScreen(
              viewModel: LoginViewModel(
                authRepository: context.read<AuthRepository>(),
              ),
            );
          },
        ),
        // A seleção de EMPRESA deixou de existir: o contexto do app é o
        // tenant, escolhido uma vez e lido por todos os produtos. Cadastro de
        // empresa nova também saiu — cliente novo nasce como tenant.
        ...tenantManagementRoutes(
          homeRoute: '/home',
          cepService: context.read<CepLookupService>(),
          onTenantSelected: (tenant) =>
              context.read<TenantSessionBridge>().onTenantSelected(tenant),
          onLogout: (routeContext) async {
            // Tudo é lido ANTES do primeiro await: depois dele o context pode
            // já não estar montado, e o logout não pode ficar pela metade.
            final auth = routeContext.read<AuthRepository>();
            final permissions = routeContext.read<PermissionNotifier>();
            final tenantPermissions =
                routeContext.read<TenantPermissionNotifier>();
            final billPaymentPermissions =
                routeContext.read<BillPaymentPermissionNotifier>();
            final tenantContext = routeContext.read<TenantContextNotifier>();
            final bridge = routeContext.read<TenantSessionBridge>();
            final reporter = routeContext.read<ErrorReporter>();
            final router = GoRouter.of(routeContext);

            await auth.logout();
            await permissions.clear();
            await tenantPermissions.clear();
            await billPaymentPermissions.clear();
            await tenantContext.clear();
            await bridge.clear();
            reporter.clearUser();
            router.go('/login');
          },
        ),
        // Contas a Pagar: o módulo fornece as telas, a casca fornece o home e o
        // seletor de arquivos — escolher arquivo depende de plugin de plataforma,
        // e o módulo não carrega dependência de plataforma.
        ...billPaymentRoutes(
          homeRoute: '/home',
          onPickDocument: _pickBillDocument,
          onOpenLink: _openBillLink,
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
              tenantContext: context.read<TenantContextNotifier>(),
              tenantSessionBridge: context.read<TenantSessionBridge>(),
              permissionNotifier: context.read<PermissionNotifier>(),
              tenantPermissionNotifier:
                  context.read<TenantPermissionNotifier>(),
              billPaymentPermissionNotifier:
                  context.read<BillPaymentPermissionNotifier>(),
              errorReporter: context.read<ErrorReporter>(),
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
          builder: (context, state) => DocumentGroupWithTemplatesScreen(
            viewModel: DocumentGroupWithTemplatesViewModel(
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
          builder: (context, state) => const EmployeeListPage(),
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
            initialTab: switch (state.uri.queryParameters['tab']) {
              'documents' => EmployeeProfileTab.documents,
              'contracts' => EmployeeProfileTab.employmentContract,
              _ => EmployeeProfileTab.personalData,
            },
            viewModel: EmployeeProfileViewModel(
              companyRepository: context.read<CompanyRepository>(),
              employeeRepository: context.read<EmployeeRepository>(),
              departmentRepository: context.read<DepartmentRepository>(),
              workplaceRepository: context.read<WorkplaceRepository>(),
              documentGroupRepository:
                  context.read<DocumentGroupRepository>(),
              cepRepository: context.read<CepRepository>(),
              scannerRepository: DocumentScannerRepositoryImpl(
                scannerService: DocumentScannerService(),
                reporter: context.read<ErrorReporter>(),
              ),
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

        // ─── Batch Document ────────────────────────────────────
        GoRoute(
          path: '/batch-document',
          builder: (context, state) {
            final companyId =
                context.read<CompanyRepository>();
            return FutureBuilder(
              future: companyId.getSelectedCompany(),
              builder: (context, snapshot) {
                final company = snapshot.data?.valueOrNull;
                if (company == null) {
                  return const Scaffold(
                    body: Center(child: CircularProgressIndicator()),
                  );
                }
                return BatchDocumentScreen(
                  viewModel: BatchDocumentViewModel(
                    batchDocumentRepository:
                        context.read<BatchDocumentRepository>(),
                    documentGroupRepository:
                        context.read<DocumentGroupRepository>(),
                    companyId: company.id,
                    scannerRepository: DocumentScannerRepositoryImpl(
                      scannerService: DocumentScannerService(),
                      reporter: context.read<ErrorReporter>(),
                    ),
                  ),
                );
              },
            );
          },
        ),

        // ─── Document Dashboard ────────────────────────────────
        GoRoute(
          path: '/document-dashboard',
          builder: (context, state) => const DocumentDashboardPage(),
        ),

        // ─── Debug ────────────────────────────────────────────────────────
        GoRoute(
          path: '/debug',
          builder: (context, state) => const DebugScreen(),
        ),

        // ─── Batch Download ────────────────────────────────────
        GoRoute(
          path: '/batch-download',
          builder: (context, state) {
            final companyRepo = context.read<CompanyRepository>();
            return FutureBuilder(
              future: companyRepo.getSelectedCompany(),
              builder: (context, snapshot) {
                final company = snapshot.data?.valueOrNull;
                if (company == null) {
                  return const Scaffold(
                    body: Center(child: CircularProgressIndicator()),
                  );
                }
                return BatchDownloadScreen(
                  viewModel: BatchDownloadViewModel(
                    batchDownloadRepository:
                        context.read<BatchDownloadRepository>(),
                    documentGroupRepository:
                        context.read<DocumentGroupRepository>(),
                    workplaceRepository:
                        context.read<WorkplaceRepository>(),
                    companyId: company.id,
                  ),
                );
              },
            );
          },
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
      builder: (context, child) => SessionExpiredListener(
        router: _router,
        navigatorKey: _rootNavigatorKey,
        child: child ?? const SizedBox.shrink(),
      ),
    );
  }
}

/// Abre o seletor de arquivos para anexar um boleto obtido à mão.
///
/// Mora na casca porque `file_picker` é plugin de plataforma: mantê-lo aqui
/// deixa o módulo de Contas a Pagar livre de dependência de plataforma, e é a
/// mesma divisão que já vale para "onde fica o home".
Future<PickedDocument?> _pickBillDocument() async {
  final result = await FilePicker.platform.pickFiles(
    type: FileType.custom,
    allowedExtensions: ['pdf', 'png', 'jpg', 'jpeg', 'webp'],
    withData: true,
  );

  final file = result?.files.singleOrNull;
  if (file?.bytes == null) return null;

  return (
    bytes: file!.bytes!,
    fileName: file.name,
    contentType: _contentTypeOf(file.extension),
  );
}

/// O tipo de mídia a partir da extensão escolhida.
///
/// O servidor recusa o que a cascata não sabe abrir, então mandar
/// `application/octet-stream` faria um PNG ser tratado como PDF — e o extrator
/// receberia imagem rotulada errado, que é o defeito já medido em 2026-08-11.
String _contentTypeOf(String? extension) => switch (extension?.toLowerCase()) {
      'png' => 'image/png',
      'jpg' || 'jpeg' => 'image/jpeg',
      'webp' => 'image/webp',
      _ => 'application/pdf',
    };

/// Abre no navegador o endereço onde o emissor publicou o boleto.
///
/// Mora na casca pelo mesmo motivo do seletor de arquivos: `url_launcher` é
/// plugin de plataforma, e o módulo de Contas a Pagar não carrega plugin.
///
/// `externalApplication` de propósito — a URL do boleto é credencial ao
/// portador, e abrir numa webview embutida deixaria a sessão do emissor dentro
/// do app, fora do navegador onde a pessoa consegue vê-la e encerrá-la.
Future<bool> _openBillLink(String url) async {
  final uri = Uri.tryParse(url);

  // Só http(s): um endereço com outro esquema entregaria ao sistema operacional
  // um alvo que não é página — e ele veio de um e-mail, não do nosso código.
  if (uri == null || (uri.scheme != 'http' && uri.scheme != 'https')) {
    return false;
  }

  try {
    return await launchUrl(uri, mode: LaunchMode.externalApplication);
  } on PlatformException {
    return false;
  }
}
