import 'package:flutter/foundation.dart';

import '../../../../domain/entities/remuneration.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/department_repository.dart';

/// Possible statuses for the role form screen.
enum RoleFormStatus { loading, idle, saving, saved, error }

/// Manages state for creating or editing a role within a position.
///
/// On construction this ViewModel immediately fetches the [PaymentUnit] and
/// [SalaryType] lookup data needed by the form dropdowns. When [positionId] is
/// non-null, [loadRole] must be called to populate the existing role fields.
class RoleFormViewModel extends ChangeNotifier {
  RoleFormViewModel({
    required CompanyRepository companyRepository,
    required DepartmentRepository departmentRepository,
    required String positionId,
  })  : _companyRepository = companyRepository,
        _departmentRepository = departmentRepository,
        _positionId = positionId;

  final CompanyRepository _companyRepository;
  final DepartmentRepository _departmentRepository;
  final String _positionId;

  String _id = '';
  String _name = '';
  String _description = '';
  String _cbo = '';
  String _paymentUnitId = '';
  String _salaryTypeId = '';
  String _baseSalaryValue = '';
  String _remunerationDescription = '';

  List<PaymentUnit> _paymentUnits = [];
  List<SalaryType> _salaryTypes = [];

  RoleFormStatus _status = RoleFormStatus.idle;
  String? _errorMessage;

  String get id => _id;
  String get name => _name;
  String get description => _description;
  String get cbo => _cbo;
  String get paymentUnitId => _paymentUnitId;
  String get salaryTypeId => _salaryTypeId;
  String get baseSalaryValue => _baseSalaryValue;
  String get remunerationDescription => _remunerationDescription;

  /// Available payment unit options for the dropdown.
  List<PaymentUnit> get paymentUnits => _paymentUnits;

  /// Available salary type options for the dropdown.
  List<SalaryType> get salaryTypes => _salaryTypes;

  RoleFormStatus get status => _status;

  /// Whether the form is loading data (lookups or existing role).
  bool get isLoading => _status == RoleFormStatus.loading;

  /// Whether the form is currently submitting data to the API.
  bool get isSaving => _status == RoleFormStatus.saving;

  /// Whether this ViewModel is in create mode (no existing role).
  bool get isNew => _id.isEmpty;

  /// Human-readable error message set when [status] is [RoleFormStatus.error].
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

  void setPaymentUnitId(String v) {
    _paymentUnitId = v;
    notifyListeners();
  }

  void setSalaryTypeId(String v) {
    _salaryTypeId = v;
    notifyListeners();
  }

  void setBaseSalaryValue(String v) {
    _baseSalaryValue = v;
  }

  void setRemunerationDescription(String v) {
    _remunerationDescription = v;
  }

  /// Fetches lookup data (payment units and salary types) from the API.
  ///
  /// Must be called before showing the form. If [roleId] is provided, also
  /// loads the existing role data from the API.
  Future<void> initialize({String? roleId}) async {
    _status = RoleFormStatus.loading;
    notifyListeners();

    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id ?? '';

    final unitsResult =
        await _departmentRepository.getPaymentUnits(companyId);
    final typesResult =
        await _departmentRepository.getSalaryTypes(companyId);

    bool lookupFailed = false;

    unitsResult.fold(
      onSuccess: (units) => _paymentUnits = units,
      onError: (_) {
        lookupFailed = true;
      },
    );

    typesResult.fold(
      onSuccess: (types) => _salaryTypes = types,
      onError: (_) {
        lookupFailed = true;
      },
    );

    if (lookupFailed) {
      _status = RoleFormStatus.error;
      _errorMessage = 'Falha ao carregar opções de remuneração.';
      notifyListeners();
      return;
    }

    if (roleId != null && roleId.isNotEmpty) {
      await loadRole(companyId, roleId);
    } else {
      _status = RoleFormStatus.idle;
      notifyListeners();
    }
  }

  /// Loads an existing role by [roleId] and populates the form fields.
  Future<void> loadRole(String companyId, String roleId) async {
    _id = roleId;

    final result = await _departmentRepository.getRoleById(companyId, roleId);
    result.fold(
      onSuccess: (role) {
        _name = role.name;
        _description = role.description;
        _cbo = role.cbo;
        _paymentUnitId = role.remuneration.paymentUnit.id;
        _salaryTypeId = role.remuneration.baseSalary.type.id;
        _baseSalaryValue = role.remuneration.baseSalary.value;
        _remunerationDescription = role.remuneration.description;
        _status = RoleFormStatus.idle;
      },
      onError: (_) {
        _status = RoleFormStatus.error;
        _errorMessage = 'Falha ao carregar dados da função.';
      },
    );
    notifyListeners();
  }

  /// Validates and submits the form, creating or updating a role.
  ///
  /// Sets [status] to [RoleFormStatus.saved] on success so the UI can
  /// navigate away. Sets [status] to [RoleFormStatus.error] on failure.
  Future<void> save() async {
    _status = RoleFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id ?? '';

    final result = _id.isEmpty
        ? await _departmentRepository.createRole(
            companyId,
            _positionId,
            name: _name,
            description: _description,
            cbo: _cbo,
            paymentUnitId: _paymentUnitId,
            salaryTypeId: _salaryTypeId,
            baseSalaryValue: _baseSalaryValue,
            remunerationDescription: _remunerationDescription,
          )
        : await _departmentRepository.updateRole(
            companyId,
            id: _id,
            name: _name,
            description: _description,
            cbo: _cbo,
            paymentUnitId: _paymentUnitId,
            salaryTypeId: _salaryTypeId,
            baseSalaryValue: _baseSalaryValue,
            remunerationDescription: _remunerationDescription,
          );

    result.fold(
      onSuccess: (_) => _status = RoleFormStatus.saved,
      onError: (_) {
        _status = RoleFormStatus.error;
        _errorMessage = 'Falha ao salvar função. Verifique os dados e tente novamente.';
      },
    );
    notifyListeners();
  }
}
