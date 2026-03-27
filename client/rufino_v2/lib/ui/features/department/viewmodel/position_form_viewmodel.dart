import 'package:flutter/widgets.dart';

import '../../../../core/utils/error_messages.dart';
import '../../../../domain/entities/position.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/department_repository.dart';

/// Possible statuses for the position form screen.
enum PositionFormStatus { loading, idle, saving, saved, error }

/// Manages state for creating or editing a position within a department.
///
/// [departmentId] must always be provided. When [positionId] is non-null,
/// [loadPosition] must be called after construction to populate the form fields.
class PositionFormViewModel extends ChangeNotifier {
  PositionFormViewModel({
    required CompanyRepository companyRepository,
    required DepartmentRepository departmentRepository,
    required String departmentId,
  })  : _companyRepository = companyRepository,
        _departmentRepository = departmentRepository,
        _departmentId = departmentId;

  final CompanyRepository _companyRepository;
  final DepartmentRepository _departmentRepository;
  final String _departmentId;

  // ─── TextEditingControllers owned by this ViewModel ────────────────────────

  /// Controller for the position name field.
  final nameController = TextEditingController();

  /// Controller for the position description field.
  final descriptionController = TextEditingController();

  /// Controller for the CBO code field.
  final cboController = TextEditingController();

  // ─── State ─────────────────────────────────────────────────────────────────

  String _id = '';
  PositionFormStatus _status = PositionFormStatus.idle;
  String? _errorMessage;
  List<String> _serverErrors = const [];

  /// Server-provided error messages extracted from the API response, if any.
  List<String> get serverErrors => _serverErrors;

  /// The id of the position being edited, empty when creating a new one.
  String get id => _id;

  /// Current status of the form operation.
  PositionFormStatus get status => _status;

  /// Whether the form is currently loading existing data from the API.
  bool get isLoading => _status == PositionFormStatus.loading;

  /// Whether the form is currently submitting data to the API.
  bool get isSaving => _status == PositionFormStatus.saving;

  /// Whether this ViewModel is in create mode (no existing position).
  bool get isNew => _id.isEmpty;

  /// Human-readable error message set when [status] is [PositionFormStatus.error].
  String? get errorMessage => _errorMessage;

  // ─── Validators — delegated to domain entity ──────────────────────────

  /// Delegates to [Position.validateName].
  String? validateName(String? v) => Position.validateName(v);

  /// Delegates to [Position.validateDescription].
  String? validateDescription(String? v) => Position.validateDescription(v);

  /// Delegates to [Position.validateCbo].
  String? validateCbo(String? v) => Position.validateCbo(v);

  // ─── Operations ────────────────────────────────────────────────────────────

  /// Loads an existing position by [positionId] and populates the form controllers.
  Future<void> loadPosition(String positionId) async {
    if (positionId.isEmpty) return;

    _id = positionId;
    _status = PositionFormStatus.loading;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final result =
          await _departmentRepository.getPositionById(companyId, positionId);
      result.fold(
        onSuccess: (position) {
          nameController.text = position.name;
          descriptionController.text = position.description;
          cboController.text = position.cbo;
          _status = PositionFormStatus.idle;
        },
        onError: (_) {
          _status = PositionFormStatus.error;
          _errorMessage = 'Falha ao carregar dados do cargo.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  /// Validates and submits the form, creating or updating a position.
  ///
  /// Sets [status] to [PositionFormStatus.saved] on success so the UI can
  /// navigate away. Sets [status] to [PositionFormStatus.error] on failure.
  Future<void> save() async {
    _status = PositionFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final result = _id.isEmpty
          ? await _departmentRepository.createPosition(
              companyId,
              _departmentId,
              name: nameController.text,
              description: descriptionController.text,
              cbo: cboController.text,
            )
          : await _departmentRepository.updatePosition(
              companyId,
              id: _id,
              name: nameController.text,
              description: descriptionController.text,
              cbo: cboController.text,
            );

      result.fold(
        onSuccess: (_) => _status = PositionFormStatus.saved,
        onError: (error) {
          _status = PositionFormStatus.error;
          _serverErrors = extractServerMessages(error);
          _errorMessage = _serverErrors.isNotEmpty
              ? _serverErrors.join('\n')
              : 'Falha ao salvar cargo. Verifique os dados e tente novamente.';
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
    cboController.dispose();
    super.dispose();
  }
}
