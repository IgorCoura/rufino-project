import 'package:flutter/foundation.dart';

import '../../../../domain/entities/department.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/department_repository.dart';

/// Possible statuses for the department list screen.
enum DepartmentListStatus { loading, idle, error }

/// Loads and exposes the list of departments for the currently selected company.
///
/// Depends on [CompanyRepository] to resolve the active company id and on
/// [DepartmentRepository] to fetch the department tree.
class DepartmentListViewModel extends ChangeNotifier {
  DepartmentListViewModel({
    required CompanyRepository companyRepository,
    required DepartmentRepository departmentRepository,
  })  : _companyRepository = companyRepository,
        _departmentRepository = departmentRepository;

  final CompanyRepository _companyRepository;
  final DepartmentRepository _departmentRepository;

  List<Department> _departments = [];
  DepartmentListStatus _status = DepartmentListStatus.idle;
  String? _errorMessage;

  /// The departments loaded from the API, empty while loading or on error.
  List<Department> get departments => _departments;

  /// Whether the list is currently being fetched.
  bool get isLoading => _status == DepartmentListStatus.loading;

  /// Whether the last fetch resulted in an error.
  bool get hasError => _status == DepartmentListStatus.error;

  /// Human-readable error message set when [hasError] is true.
  String? get errorMessage => _errorMessage;

  /// Fetches and caches the department list for the currently selected company.
  Future<void> loadDepartments() async {
    _status = DepartmentListStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id;

    if (companyId == null) {
      _status = DepartmentListStatus.error;
      _errorMessage = 'Nenhuma empresa selecionada.';
      notifyListeners();
      return;
    }

    final result = await _departmentRepository.getDepartments(companyId);
    result.fold(
      onSuccess: (data) {
        _departments = data;
        _status = DepartmentListStatus.idle;
      },
      onError: (_) {
        _departments = [];
        _status = DepartmentListStatus.error;
        _errorMessage = 'Falha ao carregar setores.';
      },
    );
    notifyListeners();
  }
}
