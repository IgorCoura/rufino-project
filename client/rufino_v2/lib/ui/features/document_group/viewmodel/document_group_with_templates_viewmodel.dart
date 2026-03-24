import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../../../domain/entities/document_group_with_templates.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/document_group_repository.dart';

/// Possible statuses for the unified document group + templates list screen.
enum DocumentGroupWithTemplatesStatus { loading, idle, error }

/// Loads and exposes document groups with their nested templates.
///
/// Depends on [CompanyRepository] to resolve the active company id and on
/// [DocumentGroupRepository] to fetch groups with templates via the
/// `withtemplates` endpoint.
class DocumentGroupWithTemplatesViewModel extends ChangeNotifier {
  DocumentGroupWithTemplatesViewModel({
    required CompanyRepository companyRepository,
    required DocumentGroupRepository documentGroupRepository,
  })  : _companyRepository = companyRepository,
        _documentGroupRepository = documentGroupRepository;

  final CompanyRepository _companyRepository;
  final DocumentGroupRepository _documentGroupRepository;

  List<DocumentGroupWithTemplates> _groups = [];
  DocumentGroupWithTemplatesStatus _status =
      DocumentGroupWithTemplatesStatus.idle;
  String? _errorMessage;

  /// The document groups with templates loaded from the API.
  UnmodifiableListView<DocumentGroupWithTemplates> get groups =>
      UnmodifiableListView(_groups);

  /// Whether the list is currently being fetched.
  bool get isLoading => _status == DocumentGroupWithTemplatesStatus.loading;

  /// Whether the last fetch resulted in an error.
  bool get hasError => _status == DocumentGroupWithTemplatesStatus.error;

  /// Human-readable error message set when [hasError] is true.
  String? get errorMessage => _errorMessage;

  /// Fetches document groups with their nested templates for the selected company.
  Future<void> loadGroups() async {
    _status = DocumentGroupWithTemplatesStatus.loading;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id;

      if (companyId == null) {
        _status = DocumentGroupWithTemplatesStatus.error;
        _errorMessage = 'Nenhuma empresa selecionada.';
        return;
      }

      final result =
          await _documentGroupRepository.getDocumentGroupsWithTemplates(
        companyId,
      );
      result.fold(
        onSuccess: (data) {
          _groups = data;
          _status = DocumentGroupWithTemplatesStatus.idle;
        },
        onError: (_) {
          _groups = [];
          _status = DocumentGroupWithTemplatesStatus.error;
          _errorMessage = 'Falha ao carregar grupos de documentos.';
        },
      );
    } finally {
      notifyListeners();
    }
  }
}
