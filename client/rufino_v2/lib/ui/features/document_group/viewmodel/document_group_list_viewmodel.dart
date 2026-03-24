import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../../../domain/entities/document_group.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/document_group_repository.dart';

/// Possible statuses for the document group list screen.
enum DocumentGroupListStatus { loading, idle, error }

/// Loads and exposes the list of document groups for the currently selected company.
///
/// Depends on [CompanyRepository] to resolve the active company id and on
/// [DocumentGroupRepository] to fetch the group list.
class DocumentGroupListViewModel extends ChangeNotifier {
  DocumentGroupListViewModel({
    required CompanyRepository companyRepository,
    required DocumentGroupRepository documentGroupRepository,
  })  : _companyRepository = companyRepository,
        _documentGroupRepository = documentGroupRepository;

  final CompanyRepository _companyRepository;
  final DocumentGroupRepository _documentGroupRepository;

  List<DocumentGroup> _groups = [];
  DocumentGroupListStatus _status = DocumentGroupListStatus.idle;
  String? _errorMessage;

  /// The document groups loaded from the API, empty while loading or on error.
  UnmodifiableListView<DocumentGroup> get groups =>
      UnmodifiableListView(_groups);

  /// Whether the list is currently being fetched.
  bool get isLoading => _status == DocumentGroupListStatus.loading;

  /// Whether the last fetch resulted in an error.
  bool get hasError => _status == DocumentGroupListStatus.error;

  /// Human-readable error message set when [hasError] is true.
  String? get errorMessage => _errorMessage;

  /// Fetches and caches the document group list for the currently selected company.
  Future<void> loadGroups() async {
    _status = DocumentGroupListStatus.loading;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id;

      if (companyId == null) {
        _status = DocumentGroupListStatus.error;
        _errorMessage = 'Nenhuma empresa selecionada.';
        return;
      }

      final result =
          await _documentGroupRepository.getDocumentGroups(companyId);
      result.fold(
        onSuccess: (data) {
          _groups = data;
          _status = DocumentGroupListStatus.idle;
        },
        onError: (_) {
          _groups = [];
          _status = DocumentGroupListStatus.error;
          _errorMessage = 'Falha ao carregar grupos de documentos.';
        },
      );
    } finally {
      notifyListeners();
    }
  }
}
