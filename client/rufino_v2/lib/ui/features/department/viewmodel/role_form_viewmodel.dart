import 'dart:collection';

import 'package:flutter/widgets.dart';

import '../../../../domain/entities/remuneration.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/department_repository.dart';

/// Possible statuses for the role form screen.
enum RoleFormStatus { loading, idle, saving, saved, error }

/// Manages state for creating or editing a role within a position.
///
/// On construction this ViewModel immediately fetches the [PaymentUnit] and
/// [SalaryType] lookup data needed by the form dropdowns. When [positionId] is
/// non-null, [initialize] must be called with [roleId] to populate the existing
/// role fields.
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

  // ─── TextEditingControllers owned by this ViewModel ────────────────────────

  /// Controller for the role name field.
  final nameController = TextEditingController();

  /// Controller for the role description field.
  final descriptionController = TextEditingController();

  /// Controller for the CBO code field.
  final cboController = TextEditingController();

  /// Controller for the base salary value field.
  final salaryValueController = TextEditingController();

  /// Controller for the remuneration description field.
  final remunerationDescriptionController = TextEditingController();

  // ─── State ─────────────────────────────────────────────────────────────────

  String _id = '';
  String _paymentUnitId = '';
  String _salaryTypeId = '';

  List<PaymentUnit> _paymentUnits = [];
  List<SalaryType> _salaryTypes = [];

  RoleFormStatus _status = RoleFormStatus.idle;
  String? _errorMessage;

  /// The id of the role being edited, empty when creating a new one.
  String get id => _id;

  /// Currently selected payment unit id (controlled by dropdown).
  String get paymentUnitId => _paymentUnitId;

  /// Currently selected salary type id (controlled by dropdown).
  String get salaryTypeId => _salaryTypeId;

  /// Available payment unit options for the dropdown.
  UnmodifiableListView<PaymentUnit> get paymentUnits =>
      UnmodifiableListView(_paymentUnits);

  /// Available salary type options for the dropdown.
  UnmodifiableListView<SalaryType> get salaryTypes =>
      UnmodifiableListView(_salaryTypes);

  /// Current status of the form operation.
  RoleFormStatus get status => _status;

  /// Whether the form is loading data (lookups or existing role).
  bool get isLoading => _status == RoleFormStatus.loading;

  /// Whether the form is currently submitting data to the API.
  bool get isSaving => _status == RoleFormStatus.saving;

  /// Whether this ViewModel is in create mode (no existing role).
  bool get isNew => _id.isEmpty;

  /// Human-readable error message set when [status] is [RoleFormStatus.error].
  String? get errorMessage => _errorMessage;

  // ─── Dropdown setters (notify listeners so dropdowns rebuild) ──────────────

  /// Sets the selected [PaymentUnit] id and notifies listeners.
  void setPaymentUnitId(String v) {
    _paymentUnitId = v;
    notifyListeners();
  }

  /// Sets the selected [SalaryType] id and notifies listeners.
  void setSalaryTypeId(String v) {
    _salaryTypeId = v;
    notifyListeners();
  }

  // ─── Validators ────────────────────────────────────────────────────────────

  /// Validates the role name: required, max 100 characters.
  String? validateName(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 100) return 'Não pode ser maior que 100 caracteres.';
    return null;
  }

  /// Validates the role description: required, max 2000 characters.
  String? validateDescription(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 2000) return 'Não pode ser maior que 2000 caracteres.';
    return null;
  }

  /// Validates the CBO code: required, max 6 characters.
  String? validateCbo(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 6) return 'Não pode ser maior que 6 caracteres.';
    return null;
  }

  /// Validates the base salary value: required, must be a valid decimal.
  String? validateSalaryValue(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    final normalized = v.replaceAll(',', '.');
    final regex = RegExp(r'^\d+(\.\d{1,2})?$');
    if (!regex.hasMatch(normalized)) return 'Valor inválido.';
    return null;
  }

  /// Validates the remuneration description: required, max 2000 characters.
  String? validateRemunerationDescription(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 2000) return 'Não pode ser maior que 2000 caracteres.';
    return null;
  }

  /// Validates the payment unit dropdown selection.
  String? validatePaymentUnit(String? v) =>
      (v == null || v.isEmpty) ? 'Selecione uma opção.' : null;

  /// Validates the salary type dropdown selection.
  String? validateSalaryType(String? v) =>
      (v == null || v.isEmpty) ? 'Selecione uma opção.' : null;

  // ─── Operations ────────────────────────────────────────────────────────────

  /// Fetches lookup data (payment units and salary types) from the API.
  ///
  /// Must be called before showing the form. If [roleId] is provided, also
  /// loads the existing role data from the API.
  Future<void> initialize({String? roleId}) async {
    _status = RoleFormStatus.loading;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final unitsResult =
          await _departmentRepository.getPaymentUnits(companyId);
      final typesResult =
          await _departmentRepository.getSalaryTypes(companyId);

      bool lookupFailed = false;

      unitsResult.fold(
        onSuccess: (units) => _paymentUnits = units,
        onError: (_) => lookupFailed = true,
      );

      typesResult.fold(
        onSuccess: (types) => _salaryTypes = types,
        onError: (_) => lookupFailed = true,
      );

      if (lookupFailed) {
        _status = RoleFormStatus.error;
        _errorMessage = 'Falha ao carregar opções de remuneração.';
        return;
      }

      if (roleId != null && roleId.isNotEmpty) {
        await _loadRole(companyId, roleId);
      } else {
        _status = RoleFormStatus.idle;
      }
    } finally {
      notifyListeners();
    }
  }

  /// Loads an existing role by [roleId] and populates the form controllers.
  Future<void> _loadRole(String companyId, String roleId) async {
    _id = roleId;

    final result = await _departmentRepository.getRoleById(companyId, roleId);
    result.fold(
      onSuccess: (role) {
        nameController.text = role.name;
        descriptionController.text = role.description;
        cboController.text = role.cbo;
        _paymentUnitId = role.remuneration.paymentUnit.id;
        _salaryTypeId = role.remuneration.baseSalary.type.id;
        salaryValueController.text = role.remuneration.baseSalary.value;
        remunerationDescriptionController.text = role.remuneration.description;
        _status = RoleFormStatus.idle;
      },
      onError: (_) {
        _status = RoleFormStatus.error;
        _errorMessage = 'Falha ao carregar dados da função.';
      },
    );
  }

  /// Validates and submits the form, creating or updating a role.
  ///
  /// Sets [status] to [RoleFormStatus.saved] on success so the UI can
  /// navigate away. Sets [status] to [RoleFormStatus.error] on failure.
  Future<void> save() async {
    _status = RoleFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final result = _id.isEmpty
          ? await _departmentRepository.createRole(
              companyId,
              _positionId,
              name: nameController.text,
              description: descriptionController.text,
              cbo: cboController.text,
              paymentUnitId: _paymentUnitId,
              salaryTypeId: _salaryTypeId,
              baseSalaryValue: salaryValueController.text,
              remunerationDescription: remunerationDescriptionController.text,
            )
          : await _departmentRepository.updateRole(
              companyId,
              id: _id,
              name: nameController.text,
              description: descriptionController.text,
              cbo: cboController.text,
              paymentUnitId: _paymentUnitId,
              salaryTypeId: _salaryTypeId,
              baseSalaryValue: salaryValueController.text,
              remunerationDescription: remunerationDescriptionController.text,
            );

      result.fold(
        onSuccess: (_) => _status = RoleFormStatus.saved,
        onError: (_) {
          _status = RoleFormStatus.error;
          _errorMessage =
              'Falha ao salvar função. Verifique os dados e tente novamente.';
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
    salaryValueController.dispose();
    remunerationDescriptionController.dispose();
    super.dispose();
  }
}
