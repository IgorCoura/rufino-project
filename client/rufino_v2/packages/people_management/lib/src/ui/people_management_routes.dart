/// The routes of the People Management product.
///
/// The shell composes; this module does not know the shell. Everything the
/// product needs from outside arrives as a parameter of
/// [peopleManagementRoutes], and every screen it owns is declared here — the
/// shell never names one.
library;

import 'package:go_router/go_router.dart';

import '../domain/ports/document_scanner_service.dart';
import '../domain/ports/file_picker_service.dart';
import 'document_dashboard/widgets/document_dashboard_screen.dart';
import 'employee/viewmodel/employee_profile_viewmodel.dart';
import 'employee/widgets/employee_list_screen.dart';
import 'people_management_pages.dart';

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
      builder: (context, state) =>
          CompanyFormPage(companyId: state.pathParameters['id']),
    ),
    GoRoute(
      path: '/department',
      builder: (context, state) => const DepartmentListPage(),
    ),
    GoRoute(
      path: '/department/create',
      builder: (context, state) => const DepartmentFormPage(),
    ),
    GoRoute(
      path: '/department/edit/:id',
      builder: (context, state) =>
          DepartmentFormPage(departmentId: state.pathParameters['id']),
    ),
    GoRoute(
      path: '/department/position/create/:departmentId',
      builder: (context, state) => PositionFormPage(
        departmentId: state.pathParameters['departmentId']!,
      ),
    ),
    GoRoute(
      path: '/department/position/edit/:departmentId/:id',
      builder: (context, state) => PositionFormPage(
        departmentId: state.pathParameters['departmentId']!,
        positionId: state.pathParameters['id'],
      ),
    ),
    GoRoute(
      path: '/department/role/create/:positionId',
      builder: (context, state) => RoleFormPage(
        positionId: state.pathParameters['positionId']!,
      ),
    ),
    GoRoute(
      path: '/department/role/edit/:positionId/:id',
      builder: (context, state) => RoleFormPage(
        positionId: state.pathParameters['positionId']!,
        roleId: state.pathParameters['id'],
      ),
    ),
    GoRoute(
      path: '/document-group',
      builder: (context, state) => const DocumentGroupWithTemplatesPage(),
    ),
    GoRoute(
      path: '/document-group/create',
      builder: (context, state) => const DocumentGroupFormPage(),
    ),
    GoRoute(
      path: '/document-group/edit/:id',
      builder: (context, state) =>
          DocumentGroupFormPage(groupId: state.pathParameters['id']),
    ),
    GoRoute(
      path: '/document-template/create',
      builder: (context, state) =>
          DocumentTemplateFormPage(filePicker: filePicker),
    ),
    GoRoute(
      path: '/document-template/edit/:id',
      builder: (context, state) => DocumentTemplateFormPage(
        filePicker: filePicker,
        templateId: state.pathParameters['id'],
      ),
    ),
    GoRoute(
      path: '/require-document',
      builder: (context, state) => const RequireDocumentListPage(),
    ),
    GoRoute(
      path: '/require-document/create',
      builder: (context, state) => const RequireDocumentFormPage(),
    ),
    GoRoute(
      path: '/require-document/edit/:id',
      builder: (context, state) => RequireDocumentFormPage(
        requireDocumentId: state.pathParameters['id'],
      ),
    ),
    GoRoute(
      path: '/employee',
      builder: (context, state) => const EmployeeListPage(),
    ),
    GoRoute(
      path: '/employee/create',
      builder: (context, state) => const EmployeeFormPage(),
    ),
    GoRoute(
      path: '/employee/:id',
      builder: (context, state) => EmployeeProfilePage(
        employeeId: state.pathParameters['id']!,
        initialTab: switch (state.uri.queryParameters['tab']) {
          'documents' => EmployeeProfileTab.documents,
          'contracts' => EmployeeProfileTab.employmentContract,
          _ => EmployeeProfileTab.personalData,
        },
        scannerService: scannerService,
      ),
    ),
    GoRoute(
      path: '/workplace',
      builder: (context, state) => const WorkplaceListPage(),
    ),
    GoRoute(
      path: '/workplace/create',
      builder: (context, state) => const WorkplaceFormPage(),
    ),
    GoRoute(
      path: '/workplace/edit/:id',
      builder: (context, state) =>
          WorkplaceFormPage(workplaceId: state.pathParameters['id']),
    ),
    GoRoute(
      path: '/batch-document',
      builder: (context, state) =>
          BatchDocumentPage(scannerService: scannerService),
    ),
    GoRoute(
      path: '/document-dashboard',
      builder: (context, state) => const DocumentDashboardPage(),
    ),
    GoRoute(
      path: '/batch-download',
      builder: (context, state) => const BatchDownloadPage(),
    ),
  ];
