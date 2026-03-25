import 'package:flutter/widgets.dart';

import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/department_repository.dart';

/// Possible statuses for the department form screen.
enum DepartmentFormStatus { loading, idle, saving, saved, error }

/// Manages state for creating or editing a department.
///
/// When [departmentId] is non-null, [loadDepartment] must be called after
/// construction to populate the form fields from the API.
class DepartmentFormViewModel extends ChangeNotifier {
  DepartmentFormViewModel({
    required CompanyRepository companyRepository,
    required DepartmentRepository departmentRepository,
  })  : _companyRepository = companyRepository,
        _departmentRepository = departmentRepository;

  final CompanyRepository _companyRepository;
  final DepartmentRepository _departmentRepository;

  // ─── TextEditingControllers owned by this ViewModel ────────────────────────

  /// Controller for the department name field.
  final nameController = TextEditingController();

  /// Controller for the department description field.
  final descriptionController = TextEditingController();

  // ─── State ─────────────────────────────────────────────────────────────────

  String _id = '';
  DepartmentFormStatus _status = DepartmentFormStatus.idle;
  String? _errorMessage;

  /// The id of the department being edited, empty when creating a new one.
  String get id => _id;

  /// Current status of the form operation.
  DepartmentFormStatus get status => _status;

  /// Whether the form is currently loading existing data from the API.
  bool get isLoading => _status == DepartmentFormStatus.loading;

  /// Whether the form is currently submitting data to the API.
  bool get isSaving => _status == DepartmentFormStatus.saving;

  /// Whether this ViewModel is in create mode (no existing department).
  bool get isNew => _id.isEmpty;

  /// Human-readable error message set when [status] is [DepartmentFormStatus.error].
  String? get errorMessage => _errorMessage;

  // ─── Validators ────────────────────────────────────────────────────────────

  /// Validates the department name: required, max 100 characters.
  String? validateName(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 100) return 'Não pode ser maior que 100 caracteres.';
    return null;
  }

  /// Validates the department description: required, max 2000 characters.
  String? validateDescription(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 2000) return 'Não pode ser maior que 2000 caracteres.';
    return null;
  }

  // ─── Operations ────────────────────────────────────────────────────────────

  /// Loads an existing department by [departmentId] and populates the form controllers.
  Future<void> loadDepartment(String departmentId) async {
    if (departmentId.isEmpty) return;

    _id = departmentId;
    _status = DepartmentFormStatus.loading;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final result = await _departmentRepository.getDepartmentById(
          companyId, departmentId);
      result.fold(
        onSuccess: (department) {
          nameController.text = department.name;
          descriptionController.text = department.description;
          _status = DepartmentFormStatus.idle;
        },
        onError: (_) {
          _status = DepartmentFormStatus.error;
          _errorMessage = 'Falha ao carregar dados do setor.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  /// Validates and submits the form, creating or updating a department.
  ///
  /// Sets [status] to [DepartmentFormStatus.saved] on success so the UI can
  /// navigate away. Sets [status] to [DepartmentFormStatus.error] on failure.
  Future<void> save() async {
    _status = DepartmentFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final result = _id.isEmpty
          ? await _departmentRepository.createDepartment(
              companyId,
              name: nameController.text,
              description: descriptionController.text,
            )
          : await _departmentRepository.updateDepartment(
              companyId,
              id: _id,
              name: nameController.text,
              description: descriptionController.text,
            );

      result.fold(
        onSuccess: (_) => _status = DepartmentFormStatus.saved,
        onError: (_) {
          _status = DepartmentFormStatus.error;
          _errorMessage =
              'Falha ao salvar setor. Verifique os dados e tente novamente.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  @override
  void dispose() {
    nameController.dispose();
    descriptionController.dispose();
    super.dispose();
  }
}
