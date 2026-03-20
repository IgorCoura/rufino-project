import 'package:flutter/foundation.dart';

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

  String _id = '';
  String _name = '';
  String _zipCode = '';
  String _street = '';
  String _number = '';
  String _complement = '';
  String _neighborhood = '';
  String _city = '';
  String _state = '';
  String _country = '';
  WorkplaceFormStatus _status = WorkplaceFormStatus.idle;
  String? _errorMessage;

  /// The id of the workplace being edited, empty when creating a new one.
  String get id => _id;
  String get name => _name;
  String get zipCode => _zipCode;
  String get street => _street;
  String get number => _number;
  String get complement => _complement;
  String get neighborhood => _neighborhood;
  String get city => _city;
  String get state => _state;
  String get country => _country;
  WorkplaceFormStatus get status => _status;

  /// Whether the form is currently loading existing data from the API.
  bool get isLoading => _status == WorkplaceFormStatus.loading;

  /// Whether the form is currently submitting data to the API.
  bool get isSaving => _status == WorkplaceFormStatus.saving;

  /// Whether this ViewModel is in create mode (no existing workplace).
  bool get isNew => _id.isEmpty;

  /// Human-readable error message set when [status] is [WorkplaceFormStatus.error].
  String? get errorMessage => _errorMessage;

  void setName(String v) => _name = v;
  void setZipCode(String v) => _zipCode = v;
  void setStreet(String v) => _street = v;
  void setNumber(String v) => _number = v;
  void setComplement(String v) => _complement = v;
  void setNeighborhood(String v) => _neighborhood = v;
  void setCity(String v) => _city = v;
  void setState(String v) => _state = v;
  void setCountry(String v) => _country = v;

  /// Loads an existing workplace by [workplaceId] and populates the form fields.
  Future<void> loadWorkplace(String workplaceId) async {
    if (workplaceId.isEmpty) return;

    _id = workplaceId;
    _status = WorkplaceFormStatus.loading;
    notifyListeners();

    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id ?? '';

    final result =
        await _workplaceRepository.getWorkplaceById(companyId, workplaceId);
    result.fold(
      onSuccess: (workplace) {
        _name = workplace.name;
        _zipCode = workplace.address.zipCode;
        _street = workplace.address.street;
        _number = workplace.address.number;
        _complement = workplace.address.complement;
        _neighborhood = workplace.address.neighborhood;
        _city = workplace.address.city;
        _state = workplace.address.state;
        _country = workplace.address.country;
        _status = WorkplaceFormStatus.idle;
      },
      onError: (_) {
        _status = WorkplaceFormStatus.error;
        _errorMessage = 'Falha ao carregar dados do local de trabalho.';
      },
    );
    notifyListeners();
  }

  /// Validates and submits the form, creating or updating a workplace.
  ///
  /// Sets [status] to [WorkplaceFormStatus.saved] on success so the UI can
  /// navigate away. Sets [status] to [WorkplaceFormStatus.error] on failure.
  Future<void> save() async {
    _status = WorkplaceFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id ?? '';

    final result = _id.isEmpty
        ? await _workplaceRepository.createWorkplace(
            companyId,
            name: _name,
            zipCode: _zipCode,
            street: _street,
            number: _number,
            complement: _complement,
            neighborhood: _neighborhood,
            city: _city,
            state: _state,
            country: _country,
          )
        : await _workplaceRepository.updateWorkplace(
            companyId,
            id: _id,
            name: _name,
            zipCode: _zipCode,
            street: _street,
            number: _number,
            complement: _complement,
            neighborhood: _neighborhood,
            city: _city,
            state: _state,
            country: _country,
          );

    result.fold(
      onSuccess: (_) => _status = WorkplaceFormStatus.saved,
      onError: (_) {
        _status = WorkplaceFormStatus.error;
        _errorMessage =
            'Falha ao salvar local de trabalho. Verifique os dados e tente novamente.';
      },
    );
    notifyListeners();
  }
}
