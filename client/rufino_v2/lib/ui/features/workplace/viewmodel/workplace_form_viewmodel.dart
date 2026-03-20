import 'package:flutter/widgets.dart';

import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/workplace_repository.dart';

/// Possible statuses for the workplace form screen.
enum WorkplaceFormStatus { loading, idle, saving, saved, error }

/// Manages state for creating or editing a workplace.
///
/// When [workplaceId] is non-null, [loadWorkplace] must be called after
/// construction to populate the form fields from the API.
class WorkplaceFormViewModel extends ChangeNotifier {
  WorkplaceFormViewModel({
    required CompanyRepository companyRepository,
    required WorkplaceRepository workplaceRepository,
  })  : _companyRepository = companyRepository,
        _workplaceRepository = workplaceRepository;

  final CompanyRepository _companyRepository;
  final WorkplaceRepository _workplaceRepository;

  // ─── TextEditingControllers owned by this ViewModel ────────────────────────

  /// Controller for the workplace name field.
  final nameController = TextEditingController();

  /// Controller for the ZIP code field.
  final zipCodeController = TextEditingController();

  /// Controller for the street field.
  final streetController = TextEditingController();

  /// Controller for the number field.
  final numberController = TextEditingController();

  /// Controller for the complement field (optional).
  final complementController = TextEditingController();

  /// Controller for the neighborhood field.
  final neighborhoodController = TextEditingController();

  /// Controller for the city field.
  final cityController = TextEditingController();

  /// Controller for the state field.
  final stateController = TextEditingController();

  /// Controller for the country field.
  final countryController = TextEditingController();

  // ─── State ─────────────────────────────────────────────────────────────────

  String _id = '';
  WorkplaceFormStatus _status = WorkplaceFormStatus.idle;
  String? _errorMessage;

  /// The id of the workplace being edited, empty when creating a new one.
  String get id => _id;

  /// Current status of the form operation.
  WorkplaceFormStatus get status => _status;

  /// Whether the form is currently loading existing data from the API.
  bool get isLoading => _status == WorkplaceFormStatus.loading;

  /// Whether the form is currently submitting data to the API.
  bool get isSaving => _status == WorkplaceFormStatus.saving;

  /// Whether this ViewModel is in create mode (no existing workplace).
  bool get isNew => _id.isEmpty;

  /// Human-readable error message set when [status] is [WorkplaceFormStatus.error].
  String? get errorMessage => _errorMessage;

  // ─── Validators ────────────────────────────────────────────────────────────

  /// Validates the workplace name: required, max 100 characters.
  String? validateName(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 100) return 'Não pode ser maior que 100 caracteres.';
    return null;
  }

  /// Validates the ZIP code: required, exactly 8 digits.
  String? validateZipCode(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    final digits = v.replaceAll(RegExp(r'\D'), '');
    if (digits.length != 8) return 'CEP inválido.';
    return null;
  }

  /// Validates the street: required, max 100 characters.
  String? validateStreet(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 100) return 'Máx. 100 caracteres.';
    return null;
  }

  /// Validates the number: required.
  String? validateNumber(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    return null;
  }

  /// Validates the complement: optional, max 50 characters.
  String? validateComplement(String? v) {
    if (v != null && v.length > 50) return 'Máx. 50 caracteres.';
    return null;
  }

  /// Validates the neighborhood: required, max 50 characters.
  String? validateNeighborhood(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 50) return 'Máx. 50 caracteres.';
    return null;
  }

  /// Validates the city: required, max 50 characters.
  String? validateCity(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 50) return 'Máx. 50 caracteres.';
    return null;
  }

  /// Validates the state: required, max 50 characters.
  String? validateState(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 50) return 'Máx. 50 caracteres.';
    return null;
  }

  /// Validates the country: required, max 50 characters.
  String? validateCountry(String? v) {
    if (v == null || v.isEmpty) return 'Não pode ser vazio.';
    if (v.length > 50) return 'Máx. 50 caracteres.';
    return null;
  }

  // ─── Operations ────────────────────────────────────────────────────────────

  /// Loads an existing workplace by [workplaceId] and populates the form controllers.
  Future<void> loadWorkplace(String workplaceId) async {
    if (workplaceId.isEmpty) return;

    _id = workplaceId;
    _status = WorkplaceFormStatus.loading;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final result =
          await _workplaceRepository.getWorkplaceById(companyId, workplaceId);
      result.fold(
        onSuccess: (workplace) {
          nameController.text = workplace.name;
          zipCodeController.text = workplace.address.zipCode;
          streetController.text = workplace.address.street;
          numberController.text = workplace.address.number;
          complementController.text = workplace.address.complement;
          neighborhoodController.text = workplace.address.neighborhood;
          cityController.text = workplace.address.city;
          stateController.text = workplace.address.state;
          countryController.text = workplace.address.country;
          _status = WorkplaceFormStatus.idle;
        },
        onError: (_) {
          _status = WorkplaceFormStatus.error;
          _errorMessage = 'Falha ao carregar dados do local de trabalho.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  /// Validates and submits the form, creating or updating a workplace.
  ///
  /// Sets [status] to [WorkplaceFormStatus.saved] on success so the UI can
  /// navigate away. Sets [status] to [WorkplaceFormStatus.error] on failure.
  Future<void> save() async {
    _status = WorkplaceFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final result = _id.isEmpty
          ? await _workplaceRepository.createWorkplace(
              companyId,
              name: nameController.text,
              zipCode: zipCodeController.text,
              street: streetController.text,
              number: numberController.text,
              complement: complementController.text,
              neighborhood: neighborhoodController.text,
              city: cityController.text,
              state: stateController.text,
              country: countryController.text,
            )
          : await _workplaceRepository.updateWorkplace(
              companyId,
              id: _id,
              name: nameController.text,
              zipCode: zipCodeController.text,
              street: streetController.text,
              number: numberController.text,
              complement: complementController.text,
              neighborhood: neighborhoodController.text,
              city: cityController.text,
              state: stateController.text,
              country: countryController.text,
            );

      result.fold(
        onSuccess: (_) => _status = WorkplaceFormStatus.saved,
        onError: (_) {
          _status = WorkplaceFormStatus.error;
          _errorMessage =
              'Falha ao salvar local de trabalho. Verifique os dados e tente novamente.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  @override
  void dispose() {
    nameController.dispose();
    zipCodeController.dispose();
    streetController.dispose();
    numberController.dispose();
    complementController.dispose();
    neighborhoodController.dispose();
    cityController.dispose();
    stateController.dispose();
    countryController.dispose();
    super.dispose();
  }
}
