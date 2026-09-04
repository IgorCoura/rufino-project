import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:people_management/people_management.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fake_batch_download_repository.dart';
import '../fakes/fake_company_repository.dart';
import '../fakes/fake_department_repository.dart';
import '../fakes/fake_document_group_repository.dart';
import '../fakes/fake_file_picker_service.dart';
import '../fakes/fake_permission_repository.dart';
import '../fakes/fake_require_document_repository.dart';
import '../fakes/fake_workplace_repository.dart';

/// Testes de regressão do ciclo de vida do ViewModel nas rotas do produto.
///
/// O bug que eles reproduzem: enquanto o ViewModel era criado dentro do
/// `builder` da rota, cada `push`/`pop` fazia nascer uma instância nova em
/// estado de carregamento — e como o `State` da tela sobrevive ao rebuild, o
/// `initState` que dispara o carregamento não roda de novo. A tela voltava e
/// ficava girando para sempre, sem erro e sem log.
///
/// **O formato importa.** Um teste de widget que monta a tela uma vez passa nos
/// dois desenhos; só a navegação de ida e volta com um `GoRouter` de verdade
/// separa um do outro. Por isso estes testes usam `peopleManagementRoutes`, e
/// não uma rota montada à mão.
const _company = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _address = Address(
  zipCode: '01310100',
  street: 'Av. Paulista',
  number: '1000',
  complement: '',
  neighborhood: 'Bela Vista',
  city: 'São Paulo',
  state: 'SP',
  country: 'Brasil',
);

const _workplace = Workplace(
  id: 'wp-1',
  name: 'Sede Principal',
  address: _address,
);

const _department = Department(
  id: 'dep-1',
  name: 'Financeiro',
  description: 'Setor financeiro',
  positions: [],
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeWorkplaceRepository workplaceRepository;
  late FakeDepartmentRepository departmentRepository;
  late FakeRequireDocumentRepository requireDocumentRepository;
  late FakeDocumentGroupRepository documentGroupRepository;
  late FakeBatchDownloadRepository batchDownloadRepository;
  late PermissionNotifier permissionNotifier;
  late GoRouter router;

  setUp(() async {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_company);
    workplaceRepository = FakeWorkplaceRepository()
      ..setWorkplaces([_workplace]);
    departmentRepository = FakeDepartmentRepository()
      ..setDepartments([_department]);
    requireDocumentRepository = FakeRequireDocumentRepository();
    documentGroupRepository = FakeDocumentGroupRepository();
    batchDownloadRepository = FakeBatchDownloadRepository();

    final permissions = FakePermissionRepository()
      ..setPermissions(const [
        Permission(
          resource: 'workplace',
          scopes: ['create', 'view', 'edit'],
        ),
        Permission(
          resource: 'department',
          scopes: ['create', 'view', 'edit'],
        ),
        Permission(resource: 'position', scopes: ['create', 'view', 'edit']),
        Permission(resource: 'role', scopes: ['create', 'view', 'edit']),
        Permission(
          resource: 'require-documents',
          scopes: ['create', 'view', 'edit'],
        ),
        Permission(resource: 'document', scopes: ['view', 'download']),
      ]);
    permissionNotifier = PermissionNotifier(permissionRepository: permissions);
    await permissionNotifier.loadPermissions();
  });

  tearDown(() => permissionNotifier.dispose());

  Widget buildSubject(String initialLocation) {
    router = GoRouter(
      initialLocation: initialLocation,
      routes: [
        ...peopleManagementRoutes(
          homeRoute: '/home',
          filePicker: FakeFilePickerService(),
          scannerService: _FakeScanner(),
        ),
        GoRoute(
          path: '/home',
          builder: (_, __) => const Scaffold(body: Text('home')),
        ),
      ],
    );
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<PermissionNotifier>.value(
          value: permissionNotifier,
        ),
        Provider<CompanyRepository>.value(value: companyRepository),
        Provider<WorkplaceRepository>.value(value: workplaceRepository),
        Provider<DepartmentRepository>.value(value: departmentRepository),
        Provider<RequireDocumentRepository>.value(
          value: requireDocumentRepository,
        ),
        Provider<DocumentGroupRepository>.value(
          value: documentGroupRepository,
        ),
        Provider<BatchDownloadRepository>.value(
          value: batchDownloadRepository,
        ),
        Provider<ErrorReporter>.value(value: const NoopErrorReporter()),
      ],
      child: MaterialApp.router(routerConfig: router),
    );
  }

  group('ciclo de vida do ViewModel nas rotas', () {
    testWidgets(
        'voltar para a listagem NAO reconstroi o ViewModel — uma consulta por volta',
        (tester) async {
      await tester.pumpWidget(buildSubject('/workplace'));
      await tester.pumpAndSettle();
      expect(find.text('Sede Principal'), findsOneWidget);
      expect(workplaceRepository.getWorkplacesCallCount, 1);

      router.push('/workplace/create');
      await tester.pumpAndSettle();
      router.pop();
      await tester.pumpAndSettle();

      // UMA consulta: a da entrada. Navegar e voltar não pede dado de novo,
      // porque o ViewModel é o mesmo — é a página que o mantém vivo.
      //
      // Com o ViewModel nascendo no builder da rota, a volta construía uma
      // instância nova, que carregava por conta própria: a conta ia a dois. É
      // essa consulta a mais — e o `ChangeNotifier` abandonado junto com ela —
      // o custo que o desenho antigo cobrava a cada mudança de pilha.
      expect(workplaceRepository.getWorkplacesCallCount, 1);
      expect(find.byType(CircularProgressIndicator), findsNothing);
      expect(find.text('Sede Principal'), findsOneWidget);
    });

    testWidgets('a empresa selecionada é lida UMA vez, não a cada rebuild',
        (tester) async {
      await tester.pumpWidget(buildSubject('/batch-download'));
      await tester.pumpAndSettle();
      final afterFirstBuild = companyRepository.getSelectedCompanyCallCount;
      expect(afterFirstBuild, 1);

      // Empilhar e desempilhar reexecuta o builder da rota. Enquanto o
      // `Future` nascia lá dentro, cada volta disparava uma leitura nova do
      // armazenamento e a tela piscava para o indicador antes de reconstruir
      // o ViewModel inteiro.
      router.push('/home');
      await tester.pumpAndSettle();
      router.pop();
      await tester.pumpAndSettle();

      expect(companyRepository.getSelectedCompanyCallCount, afterFirstBuild);
      expect(find.byType(CircularProgressIndicator), findsNothing);
    });
  });
}

/// Digitalizador de teste: as rotas exigem a porta, nenhum destes testes a usa.
class _FakeScanner implements DocumentScannerService {
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
