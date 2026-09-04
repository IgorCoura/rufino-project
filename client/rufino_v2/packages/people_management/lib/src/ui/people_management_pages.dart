/// As páginas do produto: cada uma é dona do ViewModel da sua tela.
///
/// Existe por um motivo só, e ele é um bug real. O `go_router` reexecuta o
/// builder da rota a cada mudança na pilha de navegação; criar o ViewModel ali
/// dentro faz nascer uma instância **nova** a cada `push`/`pop`. Como o `State`
/// da tela sobrevive ao rebuild (mesmo tipo, mesma posição), o `initState` — o
/// único lugar que dispara o carregamento — **não roda de novo**, e a tela fica
/// ligada a um ViewModel que ninguém mandou carregar: spinner eterno, sem erro
/// e sem nada no log.
///
/// Aqui o ViewModel nasce uma vez, no `initState` da página, e é descartado no
/// `dispose` — que no formato antigo nunca era chamado: cada rebuild abandonava
/// um `ChangeNotifier` sem descartar.
///
/// O builder da rota só constrói a página. Mesma disciplina de
/// `bill_payment_pages.dart` e `tenant_pages.dart`.
library;

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../domain/ports/document_scanner_service.dart';
import '../domain/ports/file_picker_service.dart';
import '../domain/repositories/batch_document_repository.dart';
import '../domain/repositories/batch_download_repository.dart';
import '../domain/repositories/cep_repository.dart';
import '../domain/repositories/company_repository.dart';
import '../domain/repositories/department_repository.dart';
import '../domain/repositories/document_group_repository.dart';
import '../domain/repositories/document_template_repository.dart';
import '../domain/repositories/employee_repository.dart';
import '../domain/repositories/require_document_repository.dart';
import '../domain/repositories/workplace_repository.dart';
import '../data/repositories/document_scanner_repository_impl.dart';
import 'batch_document/viewmodel/batch_document_viewmodel.dart';
import 'batch_document/widgets/batch_document_screen.dart';
import 'batch_download/viewmodel/batch_download_viewmodel.dart';
import 'batch_download/widgets/batch_download_screen.dart';
import 'company/viewmodel/company_form_viewmodel.dart';
import 'company/widgets/company_form_screen.dart';
import 'department/viewmodel/department_form_viewmodel.dart';
import 'department/viewmodel/department_list_viewmodel.dart';
import 'department/viewmodel/position_form_viewmodel.dart';
import 'department/viewmodel/role_form_viewmodel.dart';
import 'department/widgets/department_form_screen.dart';
import 'department/widgets/department_list_screen.dart';
import 'department/widgets/position_form_screen.dart';
import 'department/widgets/role_form_screen.dart';
import 'document_group/viewmodel/document_group_form_viewmodel.dart';
import 'document_group/viewmodel/document_group_with_templates_viewmodel.dart';
import 'document_group/widgets/document_group_form_screen.dart';
import 'document_group/widgets/document_group_with_templates_screen.dart';
import 'document_template/viewmodel/document_template_form_viewmodel.dart';
import 'document_template/widgets/document_template_form_screen.dart';
import 'employee/viewmodel/employee_form_viewmodel.dart';
import 'employee/viewmodel/employee_profile_viewmodel.dart';
import 'employee/widgets/employee_form_screen.dart';
import 'employee/widgets/employee_profile_screen.dart';
import 'require_document/viewmodel/require_document_form_viewmodel.dart';
import 'require_document/viewmodel/require_document_list_viewmodel.dart';
import 'require_document/widgets/require_document_form_screen.dart';
import 'require_document/widgets/require_document_list_screen.dart';
import 'workplace/viewmodel/workplace_form_viewmodel.dart';
import 'workplace/viewmodel/workplace_list_viewmodel.dart';
import 'workplace/widgets/workplace_form_screen.dart';
import 'workplace/widgets/workplace_list_screen.dart';

/// Cadastro da empresa. [companyId] nulo é criação.
class CompanyFormPage extends StatefulWidget {
  /// Cria a página.
  const CompanyFormPage({super.key, this.companyId});

  /// A empresa a editar, ou nulo para criar.
  final String? companyId;

  @override
  State<CompanyFormPage> createState() => _CompanyFormPageState();
}

class _CompanyFormPageState extends State<CompanyFormPage> {
  late final CompanyFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = CompanyFormViewModel(
      companyRepository: context.read<CompanyRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => CompanyFormScreen(
        viewModel: _viewModel,
        companyId: widget.companyId,
      );
}

/// Listagem de setores, cargos e funções.
class DepartmentListPage extends StatefulWidget {
  /// Cria a página.
  const DepartmentListPage({super.key});

  @override
  State<DepartmentListPage> createState() => _DepartmentListPageState();
}

class _DepartmentListPageState extends State<DepartmentListPage> {
  late final DepartmentListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = DepartmentListViewModel(
      companyRepository: context.read<CompanyRepository>(),
      departmentRepository: context.read<DepartmentRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      DepartmentListScreen(viewModel: _viewModel);
}

/// Cadastro de setor. [departmentId] nulo é criação.
class DepartmentFormPage extends StatefulWidget {
  /// Cria a página.
  const DepartmentFormPage({super.key, this.departmentId});

  /// O setor a editar, ou nulo para criar.
  final String? departmentId;

  @override
  State<DepartmentFormPage> createState() => _DepartmentFormPageState();
}

class _DepartmentFormPageState extends State<DepartmentFormPage> {
  late final DepartmentFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = DepartmentFormViewModel(
      companyRepository: context.read<CompanyRepository>(),
      departmentRepository: context.read<DepartmentRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => DepartmentFormScreen(
        viewModel: _viewModel,
        departmentId: widget.departmentId,
      );
}

/// Cadastro de cargo dentro de [departmentId]. [positionId] nulo é criação.
class PositionFormPage extends StatefulWidget {
  /// Cria a página.
  const PositionFormPage({
    super.key,
    required this.departmentId,
    this.positionId,
  });

  /// O setor a que o cargo pertence.
  final String departmentId;

  /// O cargo a editar, ou nulo para criar.
  final String? positionId;

  @override
  State<PositionFormPage> createState() => _PositionFormPageState();
}

class _PositionFormPageState extends State<PositionFormPage> {
  late final PositionFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = PositionFormViewModel(
      companyRepository: context.read<CompanyRepository>(),
      departmentRepository: context.read<DepartmentRepository>(),
      departmentId: widget.departmentId,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => PositionFormScreen(
        viewModel: _viewModel,
        positionId: widget.positionId,
      );
}

/// Cadastro de função dentro de [positionId]. [roleId] nulo é criação.
class RoleFormPage extends StatefulWidget {
  /// Cria a página.
  const RoleFormPage({super.key, required this.positionId, this.roleId});

  /// O cargo a que a função pertence.
  final String positionId;

  /// A função a editar, ou nulo para criar.
  final String? roleId;

  @override
  State<RoleFormPage> createState() => _RoleFormPageState();
}

class _RoleFormPageState extends State<RoleFormPage> {
  late final RoleFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = RoleFormViewModel(
      companyRepository: context.read<CompanyRepository>(),
      departmentRepository: context.read<DepartmentRepository>(),
      positionId: widget.positionId,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => RoleFormScreen(
        viewModel: _viewModel,
        roleId: widget.roleId,
      );
}

/// Grupos de documento com seus modelos.
class DocumentGroupWithTemplatesPage extends StatefulWidget {
  /// Cria a página.
  const DocumentGroupWithTemplatesPage({super.key});

  @override
  State<DocumentGroupWithTemplatesPage> createState() =>
      _DocumentGroupWithTemplatesPageState();
}

class _DocumentGroupWithTemplatesPageState
    extends State<DocumentGroupWithTemplatesPage> {
  late final DocumentGroupWithTemplatesViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = DocumentGroupWithTemplatesViewModel(
      companyRepository: context.read<CompanyRepository>(),
      documentGroupRepository: context.read<DocumentGroupRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      DocumentGroupWithTemplatesScreen(viewModel: _viewModel);
}

/// Cadastro de grupo de documento. [groupId] nulo é criação.
class DocumentGroupFormPage extends StatefulWidget {
  /// Cria a página.
  const DocumentGroupFormPage({super.key, this.groupId});

  /// O grupo a editar, ou nulo para criar.
  final String? groupId;

  @override
  State<DocumentGroupFormPage> createState() => _DocumentGroupFormPageState();
}

class _DocumentGroupFormPageState extends State<DocumentGroupFormPage> {
  late final DocumentGroupFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = DocumentGroupFormViewModel(
      companyRepository: context.read<CompanyRepository>(),
      documentGroupRepository: context.read<DocumentGroupRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => DocumentGroupFormScreen(
        viewModel: _viewModel,
        groupId: widget.groupId,
      );
}

/// Cadastro de modelo de documento. [templateId] nulo é criação.
class DocumentTemplateFormPage extends StatefulWidget {
  /// Cria a página.
  const DocumentTemplateFormPage({
    super.key,
    required this.filePicker,
    this.templateId,
  });

  /// O seletor de arquivo, que vem da casca.
  final FilePickerService filePicker;

  /// O modelo a editar, ou nulo para criar.
  final String? templateId;

  @override
  State<DocumentTemplateFormPage> createState() =>
      _DocumentTemplateFormPageState();
}

class _DocumentTemplateFormPageState extends State<DocumentTemplateFormPage> {
  late final DocumentTemplateFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = DocumentTemplateFormViewModel(
      filePicker: widget.filePicker,
      companyRepository: context.read<CompanyRepository>(),
      documentTemplateRepository: context.read<DocumentTemplateRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => DocumentTemplateFormScreen(
        viewModel: _viewModel,
        templateId: widget.templateId,
      );
}

/// Matriz de documentos exigidos.
class RequireDocumentListPage extends StatefulWidget {
  /// Cria a página.
  const RequireDocumentListPage({super.key});

  @override
  State<RequireDocumentListPage> createState() =>
      _RequireDocumentListPageState();
}

class _RequireDocumentListPageState extends State<RequireDocumentListPage> {
  late final RequireDocumentListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = RequireDocumentListViewModel(
      companyRepository: context.read<CompanyRepository>(),
      requireDocumentRepository: context.read<RequireDocumentRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      RequireDocumentListScreen(viewModel: _viewModel);
}

/// Cadastro de documento exigido. [requireDocumentId] nulo é criação.
class RequireDocumentFormPage extends StatefulWidget {
  /// Cria a página.
  const RequireDocumentFormPage({super.key, this.requireDocumentId});

  /// O requerimento a editar, ou nulo para criar.
  final String? requireDocumentId;

  @override
  State<RequireDocumentFormPage> createState() =>
      _RequireDocumentFormPageState();
}

class _RequireDocumentFormPageState extends State<RequireDocumentFormPage> {
  late final RequireDocumentFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = RequireDocumentFormViewModel(
      companyRepository: context.read<CompanyRepository>(),
      requireDocumentRepository: context.read<RequireDocumentRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => RequireDocumentFormScreen(
        viewModel: _viewModel,
        requireDocumentId: widget.requireDocumentId,
      );
}

/// Cadastro de funcionário.
class EmployeeFormPage extends StatefulWidget {
  /// Cria a página.
  const EmployeeFormPage({super.key});

  @override
  State<EmployeeFormPage> createState() => _EmployeeFormPageState();
}

class _EmployeeFormPageState extends State<EmployeeFormPage> {
  late final EmployeeFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = EmployeeFormViewModel(
      companyRepository: context.read<CompanyRepository>(),
      departmentRepository: context.read<DepartmentRepository>(),
      workplaceRepository: context.read<WorkplaceRepository>(),
      employeeRepository: context.read<EmployeeRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      EmployeeFormScreen(viewModel: _viewModel);
}

/// Perfil do funcionário [employeeId], aberto na aba [initialTab].
class EmployeeProfilePage extends StatefulWidget {
  /// Cria a página.
  const EmployeeProfilePage({
    super.key,
    required this.employeeId,
    required this.initialTab,
    required this.scannerService,
  });

  /// O funcionário.
  final String employeeId;

  /// A aba em que a tela abre.
  final EmployeeProfileTab initialTab;

  /// O digitalizador, que vem da casca.
  final DocumentScannerService scannerService;

  @override
  State<EmployeeProfilePage> createState() => _EmployeeProfilePageState();
}

class _EmployeeProfilePageState extends State<EmployeeProfilePage> {
  late final EmployeeProfileViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = EmployeeProfileViewModel(
      companyRepository: context.read<CompanyRepository>(),
      employeeRepository: context.read<EmployeeRepository>(),
      departmentRepository: context.read<DepartmentRepository>(),
      workplaceRepository: context.read<WorkplaceRepository>(),
      documentGroupRepository: context.read<DocumentGroupRepository>(),
      cepRepository: context.read<CepRepository>(),
      scannerRepository: DocumentScannerRepositoryImpl(
        scannerService: widget.scannerService,
        reporter: context.read<ErrorReporter>(),
      ),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => EmployeeProfileScreen(
        employeeId: widget.employeeId,
        initialTab: widget.initialTab,
        viewModel: _viewModel,
      );
}

/// Listagem de locais de trabalho.
class WorkplaceListPage extends StatefulWidget {
  /// Cria a página.
  const WorkplaceListPage({super.key});

  @override
  State<WorkplaceListPage> createState() => _WorkplaceListPageState();
}

class _WorkplaceListPageState extends State<WorkplaceListPage> {
  late final WorkplaceListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = WorkplaceListViewModel(
      companyRepository: context.read<CompanyRepository>(),
      workplaceRepository: context.read<WorkplaceRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      WorkplaceListScreen(viewModel: _viewModel);
}

/// Cadastro de local de trabalho. [workplaceId] nulo é criação.
class WorkplaceFormPage extends StatefulWidget {
  /// Cria a página.
  const WorkplaceFormPage({super.key, this.workplaceId});

  /// O local a editar, ou nulo para criar.
  final String? workplaceId;

  @override
  State<WorkplaceFormPage> createState() => _WorkplaceFormPageState();
}

class _WorkplaceFormPageState extends State<WorkplaceFormPage> {
  late final WorkplaceFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = WorkplaceFormViewModel(
      companyRepository: context.read<CompanyRepository>(),
      workplaceRepository: context.read<WorkplaceRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => WorkplaceFormScreen(
        viewModel: _viewModel,
        workplaceId: widget.workplaceId,
      );
}

/// Operações de documento em lote.
///
/// Diferente das outras: o ViewModel precisa do id da empresa, que vem de uma
/// leitura assíncrona do armazenamento. O `Future` é criado **uma vez**, no
/// `initState` — no formato antigo ele nascia dentro do builder da rota, então
/// cada mudança de pilha disparava uma leitura nova e a tela piscava para o
/// indicador de carregamento antes de reconstruir tudo.
class BatchDocumentPage extends StatefulWidget {
  /// Cria a página.
  const BatchDocumentPage({super.key, required this.scannerService});

  /// O digitalizador, que vem da casca.
  final DocumentScannerService scannerService;

  @override
  State<BatchDocumentPage> createState() => _BatchDocumentPageState();
}

class _BatchDocumentPageState extends State<BatchDocumentPage> {
  late final Future<BatchDocumentViewModel?> _pending;
  BatchDocumentViewModel? _viewModel;

  @override
  void initState() {
    super.initState();
    // Os repositórios são lidos ANTES do await: depois dele o `context` pode
    // não valer mais.
    final companyRepository = context.read<CompanyRepository>();
    final batchDocumentRepository = context.read<BatchDocumentRepository>();
    final documentGroupRepository = context.read<DocumentGroupRepository>();
    final reporter = context.read<ErrorReporter>();
    _pending = companyRepository.getSelectedCompany().then((result) {
      final company = result.valueOrNull;
      if (company == null) return null;
      return _viewModel = BatchDocumentViewModel(
        batchDocumentRepository: batchDocumentRepository,
        documentGroupRepository: documentGroupRepository,
        companyId: company.id,
        scannerRepository: DocumentScannerRepositoryImpl(
          scannerService: widget.scannerService,
          reporter: reporter,
        ),
      );
    });
  }

  @override
  void dispose() {
    _viewModel?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      FutureBuilder<BatchDocumentViewModel?>(
        future: _pending,
        builder: (context, snapshot) {
          final viewModel = snapshot.data;
          if (viewModel == null) {
            return const Scaffold(
              body: Center(child: CircularProgressIndicator()),
            );
          }
          return BatchDocumentScreen(viewModel: viewModel);
        },
      );
}

/// Download de documentos em lote.
///
/// Mesmo desenho do [BatchDocumentPage], e pelo mesmo motivo.
class BatchDownloadPage extends StatefulWidget {
  /// Cria a página.
  const BatchDownloadPage({super.key});

  @override
  State<BatchDownloadPage> createState() => _BatchDownloadPageState();
}

class _BatchDownloadPageState extends State<BatchDownloadPage> {
  late final Future<BatchDownloadViewModel?> _pending;
  BatchDownloadViewModel? _viewModel;

  @override
  void initState() {
    super.initState();
    final companyRepository = context.read<CompanyRepository>();
    final batchDownloadRepository = context.read<BatchDownloadRepository>();
    final documentGroupRepository = context.read<DocumentGroupRepository>();
    final workplaceRepository = context.read<WorkplaceRepository>();
    _pending = companyRepository.getSelectedCompany().then((result) {
      final company = result.valueOrNull;
      if (company == null) return null;
      return _viewModel = BatchDownloadViewModel(
        batchDownloadRepository: batchDownloadRepository,
        documentGroupRepository: documentGroupRepository,
        workplaceRepository: workplaceRepository,
        companyId: company.id,
      );
    });
  }

  @override
  void dispose() {
    _viewModel?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      FutureBuilder<BatchDownloadViewModel?>(
        future: _pending,
        builder: (context, snapshot) {
          final viewModel = snapshot.data;
          if (viewModel == null) {
            return const Scaffold(
              body: Center(child: CircularProgressIndicator()),
            );
          }
          return BatchDownloadScreen(viewModel: viewModel);
        },
      );
}
