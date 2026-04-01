import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../../../domain/entities/company.dart';
import '../../../../domain/repositories/auth_repository.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../auth/viewmodel/permission_notifier.dart';

enum CompanySelectionStatus {
  loading,
  loaded,
  selecting,
  selected,
  noCompanies,
  error,
}

class CompanySelectionViewModel extends ChangeNotifier {
  CompanySelectionViewModel({
    required AuthRepository authRepository,
    required CompanyRepository companyRepository,
    required PermissionNotifier permissionNotifier,
  })  : _authRepository = authRepository,
        _companyRepository = companyRepository,
        _permissionNotifier = permissionNotifier;

  final AuthRepository _authRepository;
  final CompanyRepository _companyRepository;
  final PermissionNotifier _permissionNotifier;

  List<Company> _companies = [];
  Company? _selectedCompany;
  CompanySelectionStatus _status = CompanySelectionStatus.loading;
  String? _errorMessage;

  UnmodifiableListView<Company> get companies => UnmodifiableListView(_companies);
  Company? get selectedCompany => _selectedCompany;
  CompanySelectionStatus get status => _status;
  String? get errorMessage => _errorMessage;
  bool get isLoading =>
      _status == CompanySelectionStatus.loading ||
      _status == CompanySelectionStatus.selecting;

  Future<void> loadCompanies() async {
    _status = CompanySelectionStatus.loading;
    _errorMessage = null;
    notifyListeners();

    try {
      final idsResult = await _authRepository.getCompanyIds();
      if (idsResult.isError) {
        _status = CompanySelectionStatus.error;
        _errorMessage = 'Falha ao carregar empresas.';
        return;
      }

      final ids = idsResult.valueOrNull!;
      final companiesResult = await _companyRepository.getCompanies(ids);

      companiesResult.fold(
        onSuccess: (list) {
          _companies = list;
          if (list.isEmpty) {
            _status = CompanySelectionStatus.noCompanies;
          } else {
            _selectedCompany = list.first;
            _status = CompanySelectionStatus.loaded;
          }
        },
        onError: (_) {
          _status = CompanySelectionStatus.error;
          _errorMessage = 'Falha ao carregar empresas.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  void onCompanySelected(Company company) {
    _selectedCompany = company;
    notifyListeners();
  }

  Future<void> confirmSelection() async {
    if (_selectedCompany == null) return;

    _status = CompanySelectionStatus.selecting;
    notifyListeners();

    try {
      final result = await _companyRepository.selectCompany(_selectedCompany!);
      if (result.isSuccess) {
        await _permissionNotifier.loadPermissions();
        _status = CompanySelectionStatus.selected;
      } else {
        _status = CompanySelectionStatus.error;
        _errorMessage = 'Falha ao selecionar empresa.';
      }
    } finally {
      notifyListeners();
    }
  }
}
