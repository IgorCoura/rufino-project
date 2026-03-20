import 'package:flutter/foundation.dart';

import '../../../../core/result.dart';
import '../../../../domain/entities/company_detail.dart';
import '../../../../domain/repositories/company_repository.dart';

enum CompanyFormStatus { loading, idle, saving, saved, error }

class CompanyFormViewModel extends ChangeNotifier {
  CompanyFormViewModel({required CompanyRepository companyRepository})
      : _companyRepository = companyRepository;

  final CompanyRepository _companyRepository;

  String _id = '';
  String _corporateName = '';
  String _fantasyName = '';
  String _cnpj = '';
  String _email = '';
  String _phone = '';
  String _zipCode = '';
  String _street = '';
  String _number = '';
  String _complement = '';
  String _neighborhood = '';
  String _city = '';
  String _state = '';
  String _country = '';

  CompanyFormStatus _status = CompanyFormStatus.idle;
  String? _errorMessage;

  CompanyFormStatus get status => _status;
  String? get errorMessage => _errorMessage;
  bool get isLoading => _status == CompanyFormStatus.loading;
  bool get isSaving => _status == CompanyFormStatus.saving;
  bool get isNew => _id.isEmpty;

  String get corporateName => _corporateName;
  String get fantasyName => _fantasyName;
  String get cnpj => _cnpj;
  String get email => _email;
  String get phone => _phone;
  String get zipCode => _zipCode;
  String get street => _street;
  String get number => _number;
  String get complement => _complement;
  String get neighborhood => _neighborhood;
  String get city => _city;
  String get state => _state;
  String get country => _country;

  void setCorporateName(String v) { _corporateName = v; }
  void setFantasyName(String v) { _fantasyName = v; }
  void setCnpj(String v) { _cnpj = v; }
  void setEmail(String v) { _email = v; }
  void setPhone(String v) { _phone = v; }
  void setZipCode(String v) { _zipCode = v; }
  void setStreet(String v) { _street = v; }
  void setNumber(String v) { _number = v; }
  void setComplement(String v) { _complement = v; }
  void setNeighborhood(String v) { _neighborhood = v; }
  void setCity(String v) { _city = v; }
  void setState(String v) { _state = v; }
  void setCountry(String v) { _country = v; }

  Future<void> loadCompany(String companyId) async {
    if (companyId.isEmpty) return;

    _id = companyId;
    _status = CompanyFormStatus.loading;
    notifyListeners();

    final result = await _companyRepository.getCompanyDetail(companyId);
    result.fold(
      onSuccess: (company) {
        _id = company.id;
        _corporateName = company.corporateName;
        _fantasyName = company.fantasyName;
        _cnpj = company.cnpj;
        _email = company.email;
        _phone = company.phone;
        _zipCode = company.zipCode;
        _street = company.street;
        _number = company.number;
        _complement = company.complement;
        _neighborhood = company.neighborhood;
        _city = company.city;
        _state = company.state;
        _country = company.country;
        _status = CompanyFormStatus.idle;
      },
      onError: (e) {
        _status = CompanyFormStatus.error;
        _errorMessage = 'Falha ao carregar dados da empresa.';
      },
    );
    notifyListeners();
  }

  Future<void> save() async {
    _status = CompanyFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    final company = CompanyDetail(
      id: _id,
      corporateName: _corporateName,
      fantasyName: _fantasyName,
      cnpj: _cnpj,
      email: _email,
      phone: _phone,
      zipCode: _zipCode,
      street: _street,
      number: _number,
      complement: _complement,
      neighborhood: _neighborhood,
      city: _city,
      state: _state,
      country: _country,
    );

    final Result<String> result;
    if (_id.isEmpty) {
      result = await _companyRepository.createCompany(company);
    } else {
      result = await _companyRepository.updateCompany(company);
    }

    result.fold(
      onSuccess: (_) => _status = CompanyFormStatus.saved,
      onError: (_) {
        _status = CompanyFormStatus.error;
        _errorMessage = 'Falha ao salvar empresa. Verifique os dados e tente novamente.';
      },
    );
    notifyListeners();
  }
}
