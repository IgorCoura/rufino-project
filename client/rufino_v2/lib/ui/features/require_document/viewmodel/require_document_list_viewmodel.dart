import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../../../domain/entities/require_document.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/require_document_repository.dart';

/// Manages the state for the require document list screen.
///
/// Loads all require documents for the currently selected company and
/// exposes them as an immutable list.
class RequireDocumentListViewModel extends ChangeNotifier {
  RequireDocumentListViewModel({
    required CompanyRepository companyRepository,
    required RequireDocumentRepository requireDocumentRepository,
  })  : _companyRepository = companyRepository,
        _requireDocumentRepository = requireDocumentRepository;

  final CompanyRepository _companyRepository;
  final RequireDocumentRepository _requireDocumentRepository;

  List<RequireDocument> _requireDocuments = [];
  bool _isLoading = false;
  bool _hasError = false;
  String? _errorMessage;

  /// The current list of require documents.
  UnmodifiableListView<RequireDocument> get requireDocuments =>
      UnmodifiableListView(_requireDocuments);

  /// Whether data is currently being loaded from the API.
  bool get isLoading => _isLoading;

  /// Whether the last load attempt resulted in an error.
  bool get hasError => _hasError;

  /// Human-readable error message, or null if no error occurred.
  String? get errorMessage => _errorMessage;

  /// Fetches all require documents for the currently selected company.
  Future<void> loadRequireDocuments() async {
    _isLoading = true;
    _hasError = false;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final result =
          await _requireDocumentRepository.getRequireDocuments(companyId);
      result.fold(
        onSuccess: (data) => _requireDocuments = data,
        onError: (_) {
          _requireDocuments = [];
          _hasError = true;
          _errorMessage = 'Falha ao carregar requerimentos de documentos.';
        },
      );
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
