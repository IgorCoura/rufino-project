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
import 'package:people_management/people_management.dart';
import 'core/utils/document_date_extractor.dart';
import 'core/utils/document_scanner_service.dart';
import 'data/services/auth_api_service.dart';
import 'data/services/platform_file_picker_service.dart';
import 'data/services/platform_file_save_service.dart';
import 'domain/repositories/auth_repository.dart';
import 'ui/core/widgets/session_expired_listener.dart';
import 'ui/features/auth/viewmodel/auth_session_notifier.dart';
import 'ui/features/auth/viewmodel/login_sso_viewmodel.dart';
import 'ui/features/auth/viewmodel/login_viewmodel.dart';
import 'ui/features/auth/viewmodel/splash_viewmodel.dart';
import 'ui/features/auth/widgets/login_screen.dart';
import 'ui/features/auth/widgets/login_sso_screen.dart';
import 'ui/features/auth/widgets/splash_screen.dart';
import 'ui/features/debug/widgets/debug_screen.dart';
import 'ui/features/home/viewmodel/home_viewmodel.dart';
import 'ui/features/home/widgets/home_screen.dart';

/// A rota do Home. A casca é dona dela, e os módulos a recebem para saber
/// para onde o botão de voltar leva quando não há pilha.
const _homeRoute = '/home';

/// Encerra a sessão a partir de uma tela do módulo de tenants.
///
/// Vive na casca, e não no módulo, porque limpar sessão é limpar TUDO: as três
/// audiências de permissão, o contexto de tenant, a ponte e o usuário do
/// repórter de erro. Um módulo que soubesse fazer isso saberia da existência
/// dos outros dois.
///
/// Tudo é lido ANTES do primeiro await: depois dele o context pode já não estar
/// montado, e o logout não pode ficar pela metade.
Future<void> _logoutFromTenantModule(BuildContext routeContext) async {
  final auth = routeContext.read<AuthRepository>();
  final permissions = routeContext.read<PermissionNotifier>();
  final tenantPermissions = routeContext.read<TenantPermissionNotifier>();
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
}


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

    // As ferramentas de diagnóstico são do APLICATIVO, não de um produto: o
    // papel vem do token (realm role), sem chamada de rede e sem recurso em
    // API nenhuma. Ver DeveloperAccess.
    final developerAccess = DeveloperAccess(getAccessToken: getAccessToken);

    // every audience, or one of them silently goes stale.
    void reloadAllPermissions() {
      permissionNotifier.loadPermissions();
      tenantPermissionNotifier.loadPermissions();
      billPaymentPermissionNotifier.loadPermissions();
      // Relê junto: o papel vive no token, então token novo pode trazer papel
      // novo — e um diagnóstico que só aparece depois de reiniciar o app é
      // exatamente o tipo de coisa que ninguém liga ao papel.
      developerAccess.load();
    }

    if (authApiService != null) {
      authApiService.onTokenRefreshed = reloadAllPermissions;
    }
    if (authCodeApiService != null) {
      authCodeApiService.onTokenRefreshed = reloadAllPermissions;
    }

    final tenantContextNotifier = TenantContextNotifier(
      storage: secureStorage,
    );

    // As capacidades de plataforma: os módulos as recebem, não as criam.
    final spreadsheetService = SpreadsheetService();
    final fileSaveService = PlatformFileSaveService();
    final filePickerService = PlatformFilePickerService();
    final scannerService = createDocumentScannerService();

    final AuthRepository authRepository = AppConfig.useAuthorizationCodeFlow
        ? AuthCodeRepositoryImpl(
            authCodeApiService: authCodeApiService!,
            reporter: errorReporter,
          )
        : AuthRepositoryImpl(
            authApiService: authApiService!,
            reporter: errorReporter,
          );

    // ─── Os módulos ──────────────────────────────────────────────────────
    //
    // Daqui para baixo a casca não conhece produto nenhum: cada módulo traz as
    // próprias rotas, as próprias dependências e as próprias entradas de menu.
    // Ligar ou desligar um produto é acrescentar ou tirar uma linha desta
    // lista (D6).
    final peopleManagement = PeopleManagementModule(
      client: httpClient,
      baseUrl: AppConfig.peopleManagementUrl,
      getAuthHeader: getAuthHeader,
      errorReporter: errorReporter,
      storage: secureStorage,
      homeRoute: _homeRoute,
      filePicker: filePickerService,
      fileSaver: fileSaveService,
      scannerService: scannerService,
      dateExtractor: extractLastDocumentDate,
      // A ponte é da casca; o módulo só pergunta se dá para usar.
      isReady: (ctx) => ctx.watch<TenantSessionBridge>().isPeopleManagementReady,
    );

    final tenantSessionBridge = TenantSessionBridge(
      companyRepository: peopleManagement.companyRepository,
      permissionNotifier: permissionNotifier,
      tenantPermissionNotifier: tenantPermissionNotifier,
      billPaymentPermissionNotifier: billPaymentPermissionNotifier,
      errorReporter: errorReporter,
    );

    // BillPayment: o tenant corrente vai no caminho de toda rota da API.
    String getBillPaymentTenantId() {
      final tenantId = tenantContextNotifier.tenantId;
      if (tenantId == null) {
        // As rotas do módulo vivem atrás da seleção de tenant; chegar aqui
        // sem contexto é bug de navegação, não estado esperado.
        throw StateError('Nenhum cliente selecionado.');
      }
      return tenantId;
    }

    final billPayment = BillPaymentModule(
      client: httpClient,
      baseUrl: AppConfig.billPaymentUrl,
      getAuthHeader: getAuthHeader,
      getTenantId: getBillPaymentTenantId,
      errorReporter: errorReporter,
      homeRoute: _homeRoute,
      onPickDocument: _pickBillDocument,
      onOpenLink: _openBillLink,
    );

    final tenantManagement = TenantManagementModule(
      client: httpClient,
      baseUrl: AppConfig.tenantManagementUrl,
      getAuthHeader: getAuthHeader,
      errorReporter: errorReporter,
      homeRoute: _homeRoute,
      onTenantSelected: tenantSessionBridge.onTenantSelected,
      onLogout: _logoutFromTenantModule,
      cepService: CepLookupService(client: httpClient),
    );

    // A lista é o contrato inteiro entre a casca e os produtos (D6): rotas,
    // dependências e entradas de menu vêm dela, e de mais lugar nenhum.
    final modules = <AppModule>[peopleManagement, billPayment, tenantManagement];

    return [
      Provider<ErrorReporter>.value(value: errorReporter),
      ChangeNotifierProvider(create: (_) => ThemeNotifier()),
      ChangeNotifierProvider.value(value: permissionNotifier),
      ChangeNotifierProvider.value(value: developerAccess),
      ChangeNotifierProvider.value(value: tenantPermissionNotifier),
      ChangeNotifierProvider.value(value: billPaymentPermissionNotifier),
      ChangeNotifierProvider.value(value: tenantContextNotifier),
      ChangeNotifierProvider.value(value: tenantSessionBridge),
      // Consulta de CEP compartilhada: o cadastro de tenant exige endereço,
      // e a chamada é a mesma que o cadastro de funcionário já fazia.
      Provider<CepLookupService>.value(
        value: CepLookupService(client: httpClient),
      ),
      ChangeNotifierProvider.value(value: authSessionNotifier),
      Provider<AuthRepository>.value(value: authRepository),
      Provider<PermissionRepository>.value(value: permissionRepository),
      Provider<SpreadsheetService>.value(value: spreadsheetService),
      Provider<FileSaveService>.value(value: fileSaveService),
      // Cada módulo publica o que é dele — a casca não lista mais
      // repositório de produto nenhum (D6).
      for (final module in modules) ...module.providers(),
      Provider<List<AppModule>>.value(value: modules),
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
              developerAccess: context.read<DeveloperAccess>(),
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
        // As rotas dos três módulos, na ordem da lista. A casca não nomeia
        // nenhuma tela de produto (D6).
        for (final module in context.read<List<AppModule>>()) ...module.routes(),
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

        // ─── Position ─────────────────────────────────────────────────────

        // ─── Role ─────────────────────────────────────────────────────────

        // ─── Document Group ─────────────────────────────────────────────

        // ─── Document Template ────────────────────────────────────────────

        // ─── Require Document ────────────────────────────────────────────

        // ─── Employee ─────────────────────────────────────────────────────

        // ─── Workplace ────────────────────────────────────────────────────

        // ─── Batch Document ────────────────────────────────────

        // ─── Document Dashboard ────────────────────────────────

        // ─── Debug ────────────────────────────────────────────────────────
        // A rota era ALCANÇÁVEL POR URL sem verificação nenhuma: o card sumia
        // da home para quem não tinha o papel, mas digitar /debug abria a tela
        // — que mostra o AppConfig inteiro, com botão de copiar. O guard fecha
        // isso na única porta que existe.
        GoRoute(
          path: '/debug',
          redirect: (context, state) =>
              context.read<DeveloperAccess>().isDeveloper ? null : '/home',
          builder: (context, state) => const DebugScreen(),
        ),

        // ─── Batch Download ────────────────────────────────────
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
