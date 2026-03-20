import 'package:flutter/foundation.dart';

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

  String _id = '';
  String _name = '';
  String _description = '';
  String _cbo = '';
  PositionFormStatus _status = PositionFormStatus.idle;
  String? _errorMessage;

  String get id => _id;
  String get name => _name;
  String get description => _description;
  String get cbo => _cbo;
  PositionFormStatus get status => _status;

  /// Whether the form is currently loading existing data from the API.
  bool get isLoading => _status == PositionFormStatus.loading;

  /// Whether the form is currently submitting data to the API.
  bool get isSaving => _status == PositionFormStatus.saving;

  /// Whether this ViewModel is in create mode (no existing position).
  bool get isNew => _id.isEmpty;

  /// Human-readable error message set when [status] is [PositionFormStatus.error].
  String? get errorMessage => _errorMessage;

  void setName(String v) {
    _name = v;
  }

  void setDescription(String v) {
    _description = v;
  }

  void setCbo(String v) {
    _cbo = v;
  }

  /// Loads an existing position by [positionId] and populates the form fields.
  Future<void> loadPosition(String positionId) async {
    if (positionId.isEmpty) return;

    _id = positionId;
    _status = PositionFormStatus.loading;
    notifyListeners();

    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id ?? '';

    final result =
        await _departmentRepository.getPositionById(companyId, positionId);
    result.fold(
      onSuccess: (position) {
        _name = position.name;
        _description = position.description;
        _cbo = position.cbo;
        _status = PositionFormStatus.idle;
      },
      onError: (_) {
        _status = PositionFormStatus.error;
        _errorMessage = 'Falha ao carregar dados do cargo.';
      },
    );
    notifyListeners();
  }

  /// Validates and submits the form, creating or updating a position.
  ///
  /// Sets [status] to [PositionFormStatus.saved] on success so the UI can
  /// navigate away. Sets [status] to [PositionFormStatus.error] on failure.
  Future<void> save() async {
    _status = PositionFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id ?? '';

    final result = _id.isEmpty
        ? await _departmentRepository.createPosition(
            companyId,
            _departmentId,
            name: _name,
            description: _description,
            cbo: _cbo,
          )
        : await _departmentRepository.updatePosition(
            companyId,
            id: _id,
            name: _name,
            description: _description,
            cbo: _cbo,
          );

    result.fold(
      onSuccess: (_) => _status = PositionFormStatus.saved,
      onError: (_) {
        _status = PositionFormStatus.error;
        _errorMessage = 'Falha ao salvar cargo. Verifique os dados e tente novamente.';
      },
    );
    notifyListeners();
  }
}
