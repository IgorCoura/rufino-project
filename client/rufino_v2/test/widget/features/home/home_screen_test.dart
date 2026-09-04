import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:bill_payment/bill_payment.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:rufino_v2/core/tenant/tenant_session_bridge.dart';
import 'package:rufino_v2/ui/features/home/viewmodel/home_viewmodel.dart';
import 'package:rufino_v2/ui/features/home/widgets/home_screen.dart';
import 'package:tenant_management/tenant_management.dart';

import '../../../testing/fakes/fake_auth_repository.dart';
import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_error_reporter.dart';
import '../../../testing/fakes/fake_permission_repository.dart';
import '../../../testing/fakes/fake_secure_storage.dart';
import 'package:people_management/people_management.dart';
import 'package:http/http.dart' as http;
import 'dart:typed_data';

SelectedTenant _tenant({
  List<String> products = const [TenantProducts.peopleManagement],
}) {
  return SelectedTenant(
    id: 'tenant-1',
    kind: TenantKinds.company,
    legalName: 'Padaria do Zé LTDA',
    tradeName: 'Pão Quente',
    status: TenantStatuses.active,
    role: MembershipRoles.owner,
    activeProducts: products,
  );
}

void main() {
  late FakeCompanyRepository companyRepository;
  late TenantContextNotifier tenantContext;
  late TenantSessionBridge bridge;
  late PermissionNotifier permissionNotifier;
  late TenantPermissionNotifier tenantPermissionNotifier;
  late BillPaymentPermissionNotifier billPaymentPermissionNotifier;

  setUp(() {
    companyRepository = FakeCompanyRepository();
    tenantContext = TenantContextNotifier(storage: FakeSecureStorage());
  });

  Future<void> pumpHome(
    WidgetTester tester, {
    required SelectedTenant tenant,
    List<Permission> permissions = const [],
    List<Permission> tenantPermissions = const [],
    bool companyResolves = true,
  }) async {
    companyRepository.setDetailShouldFail(!companyResolves);

    final permissionRepo = FakePermissionRepository()
      ..setPermissions(permissions);
    permissionNotifier =
        PermissionNotifier(permissionRepository: permissionRepo);
    await permissionNotifier.loadPermissions();

    final tenantPermissionRepo = FakePermissionRepository()
      ..setPermissions(tenantPermissions);
    tenantPermissionNotifier =
        TenantPermissionNotifier(permissionRepository: tenantPermissionRepo);
    await tenantPermissionNotifier.loadPermissions();

    billPaymentPermissionNotifier = BillPaymentPermissionNotifier(
      permissionRepository: FakePermissionRepository(),
    );
    await billPaymentPermissionNotifier.loadPermissions();

    bridge = TenantSessionBridge(
      companyRepository: companyRepository,
      permissionNotifier: permissionNotifier,
      tenantPermissionNotifier: tenantPermissionNotifier,
      billPaymentPermissionNotifier: billPaymentPermissionNotifier,
      errorReporter: FakeErrorReporter(),
    );

    await tenantContext.select(tenant);
    await bridge.syncPeopleManagementCompany(tenant);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<PermissionNotifier>.value(
            value: permissionNotifier,
          ),
          ChangeNotifierProvider<TenantPermissionNotifier>.value(
            value: tenantPermissionNotifier,
          ),
          ChangeNotifierProvider<BillPaymentPermissionNotifier>.value(
            value: billPaymentPermissionNotifier,
          ),
          ChangeNotifierProvider<TenantContextNotifier>.value(
            value: tenantContext,
          ),
          ChangeNotifierProvider<ThemeNotifier>(create: (_) => ThemeNotifier()),
          ChangeNotifierProvider<TenantSessionBridge>.value(value: bridge),
          // O menu do Home é montado a partir desta lista (D6): cada módulo
          // responde pelas suas duas porteiras. Os módulos de verdade entram
          // aqui — é o que faz o teste cobrir a costura, e não um dublê dela.
          Provider<List<AppModule>>.value(
            value: <AppModule>[
              PeopleManagementModule(
                client: _NeverCalledClient(),
                baseUrl: 'example.test',
                getAuthHeader: () async => '',
                errorReporter: FakeErrorReporter(),
                storage: FakeSecureStorage(),
                homeRoute: '/home',
                filePicker: _UnusedFilePicker(),
                fileSaver: _UnusedFileSaver(),
                scannerService: _UnusedScanner(),
                dateExtractor: ({required bytes, required fileName}) async =>
                    null,
                isReady: (ctx) =>
                    ctx.watch<TenantSessionBridge>().isPeopleManagementReady,
              ),
              BillPaymentModule(
                client: _NeverCalledClient(),
                baseUrl: 'https://example.test',
                getAuthHeader: () async => '',
                getTenantId: () => tenant.id,
                errorReporter: FakeErrorReporter(),
                homeRoute: '/home',
                onPickDocument: () async => null,
                onOpenLink: (_) async => false,
              ),
              TenantManagementModule(
                client: _NeverCalledClient(),
                baseUrl: 'https://example.test',
                getAuthHeader: () async => '',
                errorReporter: FakeErrorReporter(),
                homeRoute: '/home',
                onTenantSelected: (_) async {},
                onLogout: (_) async {},
              ),
            ],
          ),
        ],
        child: MaterialApp(
          home: HomeScreen(
            viewModel: HomeViewModel(
              authRepository: FakeAuthRepository(),
              tenantContext: tenantContext,
              tenantSessionBridge: bridge,
              permissionNotifier: permissionNotifier,
              tenantPermissionNotifier: tenantPermissionNotifier,
              billPaymentPermissionNotifier: billPaymentPermissionNotifier,
              errorReporter: FakeErrorReporter(),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('HomeScreen', () {
    testWidgets('shows the selected customer in the app bar', (tester) async {
      await pumpHome(
        tester,
        tenant: _tenant(),
        permissions: const [
          Permission(resource: 'employee', scopes: ['view']),
        ],
      );

      expect(find.text('Pão Quente'), findsOneWidget);
      expect(find.text('Pessoa jurídica'), findsOneWidget);
    });

    testWidgets('a feature needs the product enabled AND the permission',
        (tester) async {
      await pumpHome(
        tester,
        tenant: _tenant(),
        permissions: const [
          Permission(resource: 'employee', scopes: ['view']),
        ],
      );

      expect(find.text('GESTÃO DE PESSOAS'), findsOneWidget);
      expect(find.text('Funcionários'), findsOneWidget);
      // Permissão que a pessoa não tem: o card não existe.
      expect(find.text('Setores'), findsNothing);
    });

    testWidgets('permission without the product shows nothing', (tester) async {
      await pumpHome(
        tester,
        tenant: _tenant(products: const [TenantProducts.billPayment]),
        permissions: const [
          Permission(resource: 'employee', scopes: ['view']),
        ],
      );

      expect(find.text('GESTÃO DE PESSOAS'), findsNothing);
      expect(find.text('Funcionários'), findsNothing);
    });

    testWidgets('a group with no visible entry does not render its header',
        (tester) async {
      await pumpHome(tester, tenant: _tenant());

      expect(find.text('GESTÃO DE PESSOAS'), findsNothing);
      expect(find.text('ADMINISTRAÇÃO DA PLATAFORMA'), findsNothing);
      expect(
        find.text('Nenhuma funcionalidade disponível para este cliente.'),
        findsOneWidget,
      );
    });

    testWidgets('the back-office answers to the tenant audience, not the '
        'product one', (tester) async {
      await pumpHome(
        tester,
        tenant: _tenant(),
        // Mesmo nome de recurso na audiência errada não abre nada.
        permissions: const [
          Permission(resource: TenantResources.tenant, scopes: ['view']),
        ],
      );

      expect(find.text('ADMINISTRAÇÃO DA PLATAFORMA'), findsNothing);

      await pumpHome(
        tester,
        tenant: _tenant(),
        tenantPermissions: const [
          Permission(resource: TenantResources.tenant, scopes: ['view']),
        ],
      );

      expect(find.text('ADMINISTRAÇÃO DA PLATAFORMA'), findsOneWidget);
      expect(find.text('Clientes'), findsOneWidget);
    });

    testWidgets('says People Management is not released when no company '
        'answers for the tenant', (tester) async {
      await pumpHome(
        tester,
        tenant: _tenant(),
        companyResolves: false,
        permissions: const [
          Permission(resource: 'employee', scopes: ['view']),
        ],
      );

      expect(
        find.text(
          'Gestão de Pessoas ainda não está liberada para este cliente.',
        ),
        findsOneWidget,
      );
      expect(find.text('Funcionários'), findsNothing);
    });
  });
}

/// Cliente que falha se alguém tentar usá-lo.
///
/// Os módulos montam repositórios de verdade no construtor; este teste é sobre
/// o MENU, e nenhuma chamada de rede deve sair dele. Um erro aqui é sinal de
/// que a composição passou a fazer I/O.
class _NeverCalledClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) =>
      throw StateError('o menu não faz chamada de rede: ${request.url}');
}

class _UnusedFilePicker implements FilePickerService {
  @override
  Future<List<PickedFile>> pickFiles({
    bool allowMultiple = false,
    List<String>? allowedExtensions,
  }) async =>
      const [];

  @override
  Future<String?> chooseSavePath({
    required String dialogTitle,
    required String fileName,
  }) async =>
      null;

  @override
  Future<void> writeToPath({
    required String path,
    required Uint8List bytes,
  }) async {}
}

class _UnusedFileSaver implements FileSaveService {
  @override
  Future<Result<FileSaveOutcome>> saveXlsx({
    required String fileName,
    required Uint8List bytes,
  }) async =>
      const Result.success(FileSaveOutcome.saved);

  @override
  Future<void> saveBytes({
    required String fileName,
    required Uint8List bytes,
  }) async {}
}

class _UnusedScanner implements DocumentScannerService {
  @override
  bool get isPlatformSupported => false;

  @override
  Future<void> openAppSettings() async {}

  @override
  Future<Uint8List> imagesToPdf(List<Uint8List> pages) async => Uint8List(0);

  @override
  Future<String> recognizeText(Uint8List imageBytes) async => '';

  @override
  Future<List<Uint8List>?> scanPages() async => null;
}
