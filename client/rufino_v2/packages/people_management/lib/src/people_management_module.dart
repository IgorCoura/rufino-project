import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';
import 'package:provider/single_child_widget.dart';
import 'package:rufino_core/rufino_core.dart';

import 'data/repositories/batch_document_repository_impl.dart';
import 'data/repositories/batch_download_repository_impl.dart';
import 'data/repositories/cep_repository_impl.dart';
import 'data/repositories/company_repository_impl.dart';
import 'data/repositories/department_repository_impl.dart';
import 'data/repositories/document_dashboard_repository_impl.dart';
import 'data/repositories/document_group_repository_impl.dart';
import 'data/repositories/document_template_repository_impl.dart';
import 'data/repositories/employee_repository_impl.dart';
import 'data/repositories/require_document_repository_impl.dart';
import 'data/repositories/workplace_repository_impl.dart';
import 'data/services/batch_document_api_service.dart';
import 'data/services/batch_download_api_service.dart';
import 'data/services/cep_api_service.dart';
import 'data/services/company_api_service.dart';
import 'data/services/department_api_service.dart';
import 'data/services/document_dashboard_api_service.dart';
import 'data/services/document_group_api_service.dart';
import 'data/services/document_template_api_service.dart';
import 'data/services/employee_api_service.dart';
import 'data/services/require_document_api_service.dart';
import 'data/services/workplace_api_service.dart';
import 'domain/ports/document_date_extractor.dart';
import 'domain/ports/document_scanner_service.dart';
import 'domain/ports/file_picker_service.dart';
import 'domain/ports/file_save_service.dart';
import 'domain/repositories/batch_document_repository.dart';
import 'domain/repositories/batch_download_repository.dart';
import 'domain/repositories/cep_repository.dart';
import 'domain/repositories/company_repository.dart';
import 'domain/repositories/department_repository.dart';
import 'domain/repositories/document_dashboard_repository.dart';
import 'domain/repositories/document_group_repository.dart';
import 'domain/repositories/document_template_repository.dart';
import 'domain/repositories/employee_repository.dart';
import 'domain/repositories/require_document_repository.dart';
import 'domain/repositories/workplace_repository.dart';
import 'people_management_permissions.dart';
import 'ui/people_management_routes.dart';

/// O produto de gestão de pessoas plugado na casca.
///
/// Tudo o que vem de fora chega pelo construtor — cliente HTTP, base url,
/// cabeçalho de autorização, repórter de erro, armazenamento e as quatro
/// capacidades de plataforma. Nada aqui conhece a casca.
class PeopleManagementModule extends AppModule {
  /// Cria o módulo e monta as dependências do produto.
  PeopleManagementModule({
    required this.client,
    required this.baseUrl,
    required this.getAuthHeader,
    required this.errorReporter,
    required this.storage,
    required this.homeRoute,
    required this.filePicker,
    required this.fileSaver,
    required this.scannerService,
    required this.dateExtractor,
    required this.isReady,
  })  : companyRepository = CompanyRepositoryImpl(
          companyApiService: CompanyApiService(
            client: client,
            baseUrl: baseUrl,
            getAuthHeader: getAuthHeader,
          ),
          storage: storage,
          reporter: errorReporter,
        );

  /// O cadastro local da empresa deste produto.
  ///
  /// Exposto porque a casca precisa dele para traduzir "este tenant é o
  /// contexto" em "esta é a empresa" — a ponte é costura, e costura é dela.
  /// É a interface, que já é contrato público do pacote; a implementação
  /// continua privada.
  final CompanyRepository companyRepository;

  /// O cliente HTTP da casca — com breadcrumbs e detecção de 401.
  final http.Client client;

  /// Host e porta da API deste produto.
  ///
  /// É `host:porta`, não origem completa: os services montam a URL com
  /// `Uri.https`. Não uniformize com os BCs novos sem trocar a configuração.
  final String baseUrl;

  /// Devolve o cabeçalho `Authorization` já pronto.
  final Future<String> Function() getAuthHeader;

  /// Para onde os erros vão.
  final ErrorReporter errorReporter;

  /// Onde a empresa selecionada é guardada.
  final SecureStorage storage;

  /// Rota do Home, para onde o voltar leva quando não há pilha.
  final String homeRoute;

  /// Seletor de arquivo (plugin, mora na casca).
  final FilePickerService filePicker;

  /// Salvador de arquivo (plugin, mora na casca).
  final FileSaveService fileSaver;

  /// Digitalizador (câmera + OCR, plugin, mora na casca).
  final DocumentScannerService scannerService;

  /// Leitor da data impressa no documento (OCR, plugin, mora na casca).
  final DocumentDateExtractor dateExtractor;

  /// Se o produto está pronto para uso neste tenant.
  ///
  /// O produto pode estar habilitado no cadastro e ainda assim não ter uma
  /// empresa que responda por ele — uma migração pela metade. Quem sabe disso é
  /// a casca, que faz a ponte entre o tenant e a empresa; o módulo só recebe a
  /// resposta e some do menu quando ela é falsa, em vez de mostrar cartões que
  /// devolveriam erro três toques adiante.
  final bool Function(BuildContext context) isReady;

  @override
  String get menuTitle => 'GESTÃO DE PESSOAS';

  @override
  List<RouteBase> routes() => peopleManagementRoutes(
        homeRoute: homeRoute,
        filePicker: filePicker,
        scannerService: scannerService,
      );

  @override
  List<SingleChildWidget> providers() {
    final departmentRepository = DepartmentRepositoryImpl(
      apiService: _service(DepartmentApiService.new),
      reporter: errorReporter,
    );
    final workplaceRepository = WorkplaceRepositoryImpl(
      apiService: _service(WorkplaceApiService.new),
      reporter: errorReporter,
    );
    final documentGroupRepository = DocumentGroupRepositoryImpl(
      apiService: _service(DocumentGroupApiService.new),
      reporter: errorReporter,
    );
    final documentTemplateRepository = DocumentTemplateRepositoryImpl(
      apiService: _service(DocumentTemplateApiService.new),
      reporter: errorReporter,
    );
    final requireDocumentRepository = RequireDocumentRepositoryImpl(
      apiService: _service(RequireDocumentApiService.new),
      reporter: errorReporter,
    );
    final employeeRepository = EmployeeRepositoryImpl(
      apiService: _service(EmployeeApiService.new),
      reporter: errorReporter,
    );
    final batchDocumentRepository = BatchDocumentRepositoryImpl(
      apiService: _service(BatchDocumentApiService.new),
      reporter: errorReporter,
    );
    final batchDownloadRepository = BatchDownloadRepositoryImpl(
      apiService: _service(BatchDownloadApiService.new),
      reporter: errorReporter,
    );
    final documentDashboardRepository = DocumentDashboardRepositoryImpl(
      apiService: _service(DocumentDashboardApiService.new),
      reporter: errorReporter,
    );
    // O ViaCEP é público: sem base url e sem cabeçalho.
    final cepRepository = CepRepositoryImpl(
      apiService: CepApiService(client: client),
      reporter: errorReporter,
    );

    return [
      Provider<CompanyRepository>.value(value: companyRepository),
      Provider<DepartmentRepository>.value(value: departmentRepository),
      Provider<WorkplaceRepository>.value(value: workplaceRepository),
      Provider<DocumentGroupRepository>.value(value: documentGroupRepository),
      Provider<DocumentTemplateRepository>.value(
        value: documentTemplateRepository,
      ),
      Provider<RequireDocumentRepository>.value(
        value: requireDocumentRepository,
      ),
      Provider<EmployeeRepository>.value(value: employeeRepository),
      Provider<BatchDocumentRepository>.value(value: batchDocumentRepository),
      Provider<BatchDownloadRepository>.value(value: batchDownloadRepository),
      Provider<DocumentDashboardRepository>.value(
        value: documentDashboardRepository,
      ),
      Provider<CepRepository>.value(value: cepRepository),
      Provider<FilePickerService>.value(value: filePicker),
      Provider<FileSaveService>.value(value: fileSaver),
      Provider<DocumentScannerService>.value(value: scannerService),
      Provider<DocumentDateExtractor>.value(value: dateExtractor),
    ];
  }

  /// Monta um api service deste produto — todos recebem os mesmos três.
  T _service<T>(
    T Function({
      required http.Client client,
      required String baseUrl,
      required Future<String> Function() getAuthHeader,
    }) build,
  ) =>
      build(
        client: client,
        baseUrl: baseUrl,
        getAuthHeader: getAuthHeader,
      );

  @override
  List<HomeEntry> visibleEntries(BuildContext context) {
    final tenant = context.watch<TenantContextNotifier>();
    if (!tenant.hasProduct(TenantProducts.peopleManagement)) return const [];
    if (!isReady(context)) return const [];

    // Este produto é o dono do notifier base: os guards sem parâmetro de tipo
    // resolvem para a audiência `people-management-api`.
    final permissions = context.watch<PermissionNotifier>();
    return _entries.where((e) {
      final scope = e.scope;
      return scope == null
          ? permissions.hasAnyScope(e.resource)
          : permissions.hasPermission(e.resource, scope);
    }).toList();
  }
}

const _entries = <HomeEntry>[
  HomeEntry(
    icon: Icons.people_outline,
    label: 'Funcionários',
    route: PeopleManagementRoutes.employees,
    resource: PeopleManagementResources.employee,
  ),
  HomeEntry(
    icon: Icons.location_on_outlined,
    label: 'Locais de Trabalho',
    route: PeopleManagementRoutes.workplaces,
    resource: PeopleManagementResources.workplace,
  ),
  HomeEntry(
    icon: Icons.apartment_outlined,
    label: 'Setores',
    route: PeopleManagementRoutes.departments,
    resource: PeopleManagementResources.department,
  ),
  HomeEntry(
    icon: Icons.folder_outlined,
    label: 'Grupos de Template de Documentos',
    route: PeopleManagementRoutes.documentGroups,
    resource: PeopleManagementResources.documentGroup,
  ),
  HomeEntry(
    icon: Icons.description_outlined,
    label: 'Requerimentos de Documentos',
    route: PeopleManagementRoutes.requireDocuments,
    resource: PeopleManagementResources.requireDocuments,
  ),
  HomeEntry(
    icon: Icons.insert_chart_outlined_rounded,
    label: 'Dashboard de Documentos',
    route: PeopleManagementRoutes.documentDashboard,
    resource: PeopleManagementResources.document,
  ),
  HomeEntry(
    icon: Icons.upload_file_outlined,
    label: 'Gestão de Documentos em Lote',
    route: PeopleManagementRoutes.batchDocument,
    resource: PeopleManagementResources.document,
  ),
  HomeEntry(
    icon: Icons.download_rounded,
    label: 'Download em Lote',
    route: PeopleManagementRoutes.batchDownload,
    resource: PeopleManagementResources.document,
    scope: PeopleManagementScopes.download,
  ),
];
