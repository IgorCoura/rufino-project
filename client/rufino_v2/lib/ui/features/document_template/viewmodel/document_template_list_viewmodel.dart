import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../../../domain/entities/document_template.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/document_template_repository.dart';

/// Possible statuses for the document template list screen.
enum DocumentTemplateListStatus { loading, idle, error }

/// Loads and exposes the list of document templates for the currently selected company.
///
/// Depends on [CompanyRepository] to resolve the active company id and on
/// [DocumentTemplateRepository] to fetch the template list.
class DocumentTemplateListViewModel extends ChangeNotifier {
  DocumentTemplateListViewModel({
    required CompanyRepository companyRepository,
    required DocumentTemplateRepository documentTemplateRepository,
  })  : _companyRepository = companyRepository,
        _documentTemplateRepository = documentTemplateRepository;

  final CompanyRepository _companyRepository;
  final DocumentTemplateRepository _documentTemplateRepository;

  List<DocumentTemplate> _templates = [];
  DocumentTemplateListStatus _status = DocumentTemplateListStatus.idle;
  String? _errorMessage;

  /// The document templates loaded from the API, empty while loading or on error.
  UnmodifiableListView<DocumentTemplate> get templates =>
      UnmodifiableListView(_templates);

  /// Whether the list is currently being fetched.
  bool get isLoading => _status == DocumentTemplateListStatus.loading;

  /// Whether the last fetch resulted in an error.
  bool get hasError => _status == DocumentTemplateListStatus.error;

  /// Human-readable error message set when [hasError] is true.
  String? get errorMessage => _errorMessage;

  /// Fetches and caches the document template list for the currently selected company.
  Future<void> loadTemplates() async {
    _status = DocumentTemplateListStatus.loading;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id;

      if (companyId == null) {
        _status = DocumentTemplateListStatus.error;
        _errorMessage = 'Nenhuma empresa selecionada.';
        return;
      }

      final result =
          await _documentTemplateRepository.getDocumentTemplates(companyId);
      result.fold(
        onSuccess: (data) {
          _templates = data;
          _status = DocumentTemplateListStatus.idle;
        },
        onError: (_) {
          _templates = [];
          _status = DocumentTemplateListStatus.error;
          _errorMessage = 'Falha ao carregar templates de documentos.';
        },
      );
    } finally {
      notifyListeners();
    }
  }
}
