import 'dart:collection';

import 'package:flutter/widgets.dart';

import '../../../../domain/entities/department.dart';
import '../../../../domain/entities/position.dart';
import '../../../../domain/entities/role.dart';
import '../../../../domain/entities/workplace.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/department_repository.dart';
import '../../../../domain/repositories/employee_repository.dart';
import '../../../../domain/repositories/workplace_repository.dart';

/// Possible statuses for the new employee form screen.
enum EmployeeFormStatus { loadingOptions, idle, saving, saved, error }

/// Manages state for creating a new employee.
///
/// Loads the full department hierarchy and workplace list so the user can
/// select a department → position → role cascade and a workplace before saving.
class EmployeeFormViewModel extends ChangeNotifier {
  EmployeeFormViewModel({
    required CompanyRepository companyRepository,
    required DepartmentRepository departmentRepository,
    required WorkplaceRepository workplaceRepository,
    required EmployeeRepository employeeRepository,
  })  : _companyRepository = companyRepository,
        _departmentRepository = departmentRepository,
        _workplaceRepository = workplaceRepository,
        _employeeRepository = employeeRepository;

  final CompanyRepository _companyRepository;
  final DepartmentRepository _departmentRepository;
  final WorkplaceRepository _workplaceRepository;
  final EmployeeRepository _employeeRepository;

  // ─── TextEditingControllers owned by this ViewModel ────────────────────────

  /// Controller for the employee name field.
  final nameController = TextEditingController();

  // ─── State ─────────────────────────────────────────────────────────────────

  String? _companyId;
  EmployeeFormStatus _status = EmployeeFormStatus.idle;
  String? _errorMessage;

  List<Department> _departments = [];
  List<Workplace> _workplaces = [];

  Department? _selectedDepartment;
  Position? _selectedPosition;
  Role? _selectedRole;
  Workplace? _selectedWorkplace;

  // ─── Getters ──────────────────────────────────────────────────────────────

  /// Current status of the form operation.
  EmployeeFormStatus get status => _status;

  /// Whether the form is loading its dropdown options.
  bool get isLoadingOptions => _status == EmployeeFormStatus.loadingOptions;

  /// Whether the form is currently being submitted.
  bool get isSaving => _status == EmployeeFormStatus.saving;

  /// Human-readable error message set when [status] is [EmployeeFormStatus.error].
  String? get errorMessage => _errorMessage;

  /// The available departments for the company.
  UnmodifiableListView<Department> get departments =>
      UnmodifiableListView(_departments);

  /// The workplaces available for the company.
  UnmodifiableListView<Workplace> get workplaces =>
      UnmodifiableListView(_workplaces);

  /// The positions belonging to the currently selected department.
  UnmodifiableListView<Position> get positions => UnmodifiableListView(
        _selectedDepartment?.positions ?? const [],
      );

  /// The roles belonging to the currently selected position.
  UnmodifiableListView<Role> get roles => UnmodifiableListView(
        _selectedPosition?.roles ?? const [],
      );

  /// The currently selected department, or null if none is selected.
  Department? get selectedDepartment => _selectedDepartment;

  /// The currently selected position, or null if none is selected.
  Position? get selectedPosition => _selectedPosition;

  /// The currently selected role, or null if none is selected.
  Role? get selectedRole => _selectedRole;

  /// The currently selected workplace, or null if none is selected.
  Workplace? get selectedWorkplace => _selectedWorkplace;

  // ─── Selection actions ─────────────────────────────────────────────────────

  /// Updates the selected department, resetting position and role selections.
  void onDepartmentChanged(Department? department) {
    _selectedDepartment = department;
    _selectedPosition = null;
    _selectedRole = null;
    notifyListeners();
  }

  /// Updates the selected position, resetting the role selection.
  void onPositionChanged(Position? position) {
    _selectedPosition = position;
    _selectedRole = null;
    notifyListeners();
  }

  /// Updates the selected role.
  void onRoleChanged(Role? role) {
    _selectedRole = role;
    notifyListeners();
  }

  /// Updates the selected workplace.
  void onWorkplaceChanged(Workplace? workplace) {
    _selectedWorkplace = workplace;
    notifyListeners();
  }

  // ─── Validators ────────────────────────────────────────────────────────────

  /// Validates the employee name: required, at least two words, max 100 chars.
  String? validateName(String? v) {
    if (v == null || v.trim().isEmpty) return 'O nome não pode ser vazio.';
    if (v.trim().split(RegExp(r'\s+')).length < 2) {
      return 'Informe o nome completo.';
    }
    if (v.length > 100) return 'Máx. 100 caracteres.';
    return null;
  }

  // ─── Operations ────────────────────────────────────────────────────────────

  /// Loads departments and workplaces needed to populate the form dropdowns.
  Future<void> loadOptions() async {
    _status = EmployeeFormStatus.loadingOptions;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      _companyId = companyResult.valueOrNull?.id;

      if (_companyId == null) {
        _status = EmployeeFormStatus.error;
        _errorMessage = 'Nenhuma empresa selecionada.';
        return;
      }

      final deptResult =
          await _departmentRepository.getDepartments(_companyId!);
      final wpResult =
          await _workplaceRepository.getWorkplaces(_companyId!);

      bool hasError = false;
      deptResult.fold(
        onSuccess: (data) => _departments = data,
        onError: (_) => hasError = true,
      );
      wpResult.fold(
        onSuccess: (data) => _workplaces = data,
        onError: (_) => hasError = true,
      );

      if (hasError) {
        _status = EmployeeFormStatus.error;
        _errorMessage = 'Falha ao carregar opções. Verifique a conexão.';
      } else {
        _status = EmployeeFormStatus.idle;
      }
    } finally {
      notifyListeners();
    }
  }

  /// Validates and submits the form, creating a new employee.
  ///
  /// Sets [status] to [EmployeeFormStatus.saved] on success so the UI can
  /// navigate away. Sets [status] to [EmployeeFormStatus.error] on failure.
  Future<void> save() async {
    _status = EmployeeFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    try {
      if (_companyId == null ||
          _selectedRole == null ||
          _selectedWorkplace == null) {
        _status = EmployeeFormStatus.error;
        _errorMessage = 'Selecione setor, cargo, função e local de trabalho.';
        return;
      }

      final result = await _employeeRepository.createEmployee(
        _companyId!,
        name: nameController.text.trim(),
        roleId: _selectedRole!.id,
        workplaceId: _selectedWorkplace!.id,
      );

      result.fold(
        onSuccess: (_) => _status = EmployeeFormStatus.saved,
        onError: (_) {
          _status = EmployeeFormStatus.error;
          _errorMessage =
              'Falha ao criar funcionário. Verifique os dados e tente novamente.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  @override
  void dispose() {
    nameController.dispose();
    super.dispose();
  }
}
