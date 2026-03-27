import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../core/result.dart';
import '../../../../core/utils/error_messages.dart';
import '../../../../domain/entities/address.dart';
import '../../../../domain/entities/company.dart';
import '../../../../domain/entities/company_detail.dart';
import '../../../../domain/repositories/company_repository.dart';

enum CompanyFormStatus { loading, idle, saving, saved, error }

class CompanyFormViewModel extends ChangeNotifier {
  CompanyFormViewModel({required CompanyRepository companyRepository})
      : _companyRepository = companyRepository;

  final CompanyRepository _companyRepository;

  String _id = '';

  final corporateNameController = TextEditingController();
  final fantasyNameController = TextEditingController();
  final cnpjController = TextEditingController();
  final emailController = TextEditingController();
  final phoneController = TextEditingController();
  final zipCodeController = TextEditingController();
  final streetController = TextEditingController();
  final numberController = TextEditingController();
  final complementController = TextEditingController();
  final neighborhoodController = TextEditingController();
  final cityController = TextEditingController();
  final stateController = TextEditingController();
  final countryController = TextEditingController();

  final cnpjFormatter = MaskTextInputFormatter(
    mask: '##.###.###/####-##',
    filter: {'#': RegExp(r'[0-9]')},
  );
  final phoneFormatter = MaskTextInputFormatter(
    mask: '(##) #####-####',
    filter: {'#': RegExp(r'[0-9]')},
  );
  final zipCodeFormatter = MaskTextInputFormatter(
    mask: '#####-###',
    filter: {'#': RegExp(r'[0-9]')},
  );

  CompanyFormStatus _status = CompanyFormStatus.idle;
  String? _errorMessage;
  List<String> _serverErrors = const [];

  /// Server-provided error messages extracted from the API response, if any.
  List<String> get serverErrors => _serverErrors;

  CompanyFormStatus get status => _status;
  String? get errorMessage => _errorMessage;
  bool get isLoading => _status == CompanyFormStatus.loading;
  bool get isSaving => _status == CompanyFormStatus.saving;
  bool get isNew => _id.isEmpty;

  /// Delegates to [Company.validateRequired].
  String? validateRequired(String? value) => Company.validateRequired(value);

  /// Delegates to [Company.validateEmail].
  String? validateEmail(String? value) => Company.validateEmail(value);

  /// Delegates to [Company.validateCnpj].
  String? validateCnpj(String? value) => Company.validateCnpj(value);

  /// Delegates to [Address.validateCep].
  String? validateZipCode(String? value) => Address.validateCep(value);

  Future<void> loadCompany(String companyId) async {
    if (companyId.isEmpty) return;
    _id = companyId;
    _status = CompanyFormStatus.loading;
    notifyListeners();

    try {
      final result = await _companyRepository.getCompanyDetail(companyId);
      result.fold(
        onSuccess: (company) {
          _id = company.id;
          corporateNameController.text = company.corporateName;
          fantasyNameController.text = company.fantasyName;
          cnpjController.text = company.cnpj;
          emailController.text = company.email;
          phoneController.text = company.phone;
          zipCodeController.text = company.zipCode;
          streetController.text = company.street;
          numberController.text = company.number;
          complementController.text = company.complement;
          neighborhoodController.text = company.neighborhood;
          cityController.text = company.city;
          stateController.text = company.state;
          countryController.text = company.country;
          _status = CompanyFormStatus.idle;
        },
        onError: (_) {
          _status = CompanyFormStatus.error;
          _errorMessage = 'Falha ao carregar dados da empresa.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  Future<void> save() async {
    _status = CompanyFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    try {
      final company = CompanyDetail(
        id: _id,
        corporateName: corporateNameController.text,
        fantasyName: fantasyNameController.text,
        cnpj: cnpjController.text.replaceAll(RegExp(r'\D'), ''),
        email: emailController.text,
        phone: phoneController.text.replaceAll(RegExp(r'\D'), ''),
        zipCode: zipCodeController.text.replaceAll(RegExp(r'\D'), ''),
        street: streetController.text,
        number: numberController.text,
        complement: complementController.text,
        neighborhood: neighborhoodController.text,
        city: cityController.text,
        state: stateController.text,
        country: countryController.text,
      );

      final Result<String> result;
      if (_id.isEmpty) {
        result = await _companyRepository.createCompany(company);
      } else {
        result = await _companyRepository.updateCompany(company);
      }

      result.fold(
        onSuccess: (_) => _status = CompanyFormStatus.saved,
        onError: (error) {
          _status = CompanyFormStatus.error;
          _serverErrors = extractServerMessages(error);
          _errorMessage = _serverErrors.isNotEmpty
              ? _serverErrors.join('\n')
              : 'Falha ao salvar empresa. Verifique os dados e tente novamente.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  @override
  void dispose() {
    corporateNameController.dispose();
    fantasyNameController.dispose();
    cnpjController.dispose();
    emailController.dispose();
    phoneController.dispose();
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
