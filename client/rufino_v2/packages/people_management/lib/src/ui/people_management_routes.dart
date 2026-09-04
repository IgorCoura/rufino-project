/// The routes of the People Management product.
///
/// The shell composes; this module does not know the shell. Everything the
/// product needs from outside arrives as a parameter of
/// [peopleManagementRoutes], and every screen it owns is declared here — the
/// shell never names one.
library;

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';
import '../data/repositories/document_scanner_repository_impl.dart';
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
import 'document_dashboard/widgets/document_dashboard_screen.dart';
import 'document_group/viewmodel/document_group_form_viewmodel.dart';
import 'document_group/viewmodel/document_group_with_templates_viewmodel.dart';
import 'document_group/widgets/document_group_form_screen.dart';
import 'document_group/widgets/document_group_with_templates_screen.dart';
import 'document_template/viewmodel/document_template_form_viewmodel.dart';
import 'document_template/widgets/document_template_form_screen.dart';
import 'require_document/viewmodel/require_document_form_viewmodel.dart';
import 'require_document/viewmodel/require_document_list_viewmodel.dart';
import 'require_document/widgets/require_document_form_screen.dart';
import 'require_document/widgets/require_document_list_screen.dart';
import 'workplace/viewmodel/workplace_form_viewmodel.dart';
import 'workplace/viewmodel/workplace_list_viewmodel.dart';
import 'workplace/widgets/workplace_form_screen.dart';
import 'workplace/widgets/workplace_list_screen.dart';
import 'batch_document/viewmodel/batch_document_viewmodel.dart';
import 'batch_document/widgets/batch_document_screen.dart';
import 'batch_download/viewmodel/batch_download_viewmodel.dart';
import 'batch_download/widgets/batch_download_screen.dart';
import 'employee/viewmodel/employee_form_viewmodel.dart';
import 'employee/viewmodel/employee_profile_viewmodel.dart';
import 'employee/widgets/employee_form_screen.dart';
import 'employee/widgets/employee_list_screen.dart';
import 'employee/widgets/employee_profile_screen.dart';
import '../domain/ports/file_picker_service.dart';
import '../domain/ports/document_scanner_service.dart';

/// Route paths of this product, so nobody writes them as bare strings.
///
/// The shell's home menu links to these; a typo in a literal fails at runtime,
/// a typo here does not compile.
abstract final class PeopleManagementRoutes {
  /// Employee list — the product's entry point.
  static const String employees = '/employee';

  /// Employee creation form.
  static const String employeeCreate = '/employee/create';

  /// Employee profile. [id] is the employee.
  static String employee(String id) => '/employee/$id';

  /// Workplace list.
  static const String workplaces = '/workplace';

  /// Department list (departments, positions and roles).
  static const String departments = '/department';

  /// Document group list.
  static const String documentGroups = '/document-group';

  /// Required-documents matrix.
  static const String requireDocuments = '/require-document';

  /// Document triage dashboard.
  static const String documentDashboard = '/document-dashboard';

  /// Batch document operations.
  static const String batchDocument = '/batch-document';

  /// Batch download.
  static const String batchDownload = '/batch-download';

  /// Company edit form. [id] is the company.
  static String companyEdit(String id) => '/company/edit/$id';
}

/// Builds the product's routes.
///
/// [homeRoute] is where a back button lands when there is no navigation stack
/// to pop — the shell owns the home, so it says where it is.
List<RouteBase> peopleManagementRoutes({
  required String homeRoute,
  required FilePickerService filePicker,
  required DocumentScannerService scannerService,
}) => [
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
    GoRoute(
      path: '/document-template/create',
      builder: (context, state) => DocumentTemplateFormScreen(
        viewModel: DocumentTemplateFormViewModel(
          filePicker: filePicker,
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
          filePicker: filePicker,
          companyRepository: context.read<CompanyRepository>(),
          documentTemplateRepository:
              context.read<DocumentTemplateRepository>(),
        ),
        templateId: state.pathParameters['id'],
      ),
    ),
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
            scannerService: scannerService,
            reporter: context.read<ErrorReporter>(),
          ),
        ),
      ),
    ),
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
                  scannerService: scannerService,
                  reporter: context.read<ErrorReporter>(),
                ),
              ),
            );
          },
        );
      },
    ),
    GoRoute(
      path: '/document-dashboard',
      builder: (context, state) => const DocumentDashboardPage(),
    ),
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
    ];
