import 'package:flutter/widgets.dart';

import '../../../../domain/entities/document_group.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/document_group_repository.dart';

/// Possible statuses for the document group form screen.
enum DocumentGroupFormStatus { loading, idle, saving, saved, error }

/// Manages state for creating or editing a document group.
///
/// When [id] is non-empty, [loadGroup] must be called after construction
/// to populate the form fields from the API.
class DocumentGroupFormViewModel extends ChangeNotifier {
  DocumentGroupFormViewModel({
    required CompanyRepository companyRepository,
    required DocumentGroupRepository documentGroupRepository,
  })  : _companyRepository = companyRepository,
        _documentGroupRepository = documentGroupRepository;

  final CompanyRepository _companyRepository;
  final DocumentGroupRepository _documentGroupRepository;

  // ─── TextEditingControllers owned by this ViewModel ────────────────────────

  /// Controller for the group name field.
  final nameController = TextEditingController();

  /// Controller for the group description field.
  final descriptionController = TextEditingController();

  // ─── State ─────────────────────────────────────────────────────────────────

  String _id = '';
  DocumentGroupFormStatus _status = DocumentGroupFormStatus.idle;
  String? _errorMessage;

  /// The id of the group being edited, empty when creating a new one.
  String get id => _id;

  /// Current status of the form operation.
  DocumentGroupFormStatus get status => _status;

  /// Whether the form is currently loading existing data from the API.
  bool get isLoading => _status == DocumentGroupFormStatus.loading;

  /// Whether the form is currently submitting data to the API.
  bool get isSaving => _status == DocumentGroupFormStatus.saving;

  /// Whether this ViewModel is in create mode (no existing group).
  bool get isNew => _id.isEmpty;

  /// Human-readable error message set when [status] is [DocumentGroupFormStatus.error].
  String? get errorMessage => _errorMessage;

  // ─── Validators ────────────────────────────────────────────────────────────

  /// Validates the group name: required, max 100 characters.
  String? validateName(String? v) {
    if (v == null || v.trim().isEmpty) return 'O Nome não pode ser vazio.';
    if (v.length > 100) {
      return 'O Nome não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  /// Validates the group description: required, max 1000 characters.
  String? validateDescription(String? v) {
    if (v == null || v.trim().isEmpty) {
      return 'A Descrição não pode ser vazia.';
    }
    if (v.length > 1000) {
      return 'A Descrição não pode ser maior que 1000 caracteres.';
    }
    return null;
  }

  // ─── Operations ────────────────────────────────────────────────────────────

  /// Loads an existing document group by [groupId] and populates the form controllers.
  Future<void> loadGroup(String groupId) async {
    if (groupId.isEmpty) return;

    _id = groupId;
    _status = DocumentGroupFormStatus.loading;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final result =
          await _documentGroupRepository.getDocumentGroups(companyId);
      result.fold(
        onSuccess: (groups) {
          final group = groups.where((g) => g.id == groupId).firstOrNull;
          if (group != null) {
            nameController.text = group.name;
            descriptionController.text = group.description;
            _status = DocumentGroupFormStatus.idle;
          } else {
            _status = DocumentGroupFormStatus.error;
            _errorMessage = 'Grupo de documentos não encontrado.';
          }
        },
        onError: (_) {
          _status = DocumentGroupFormStatus.error;
          _errorMessage = 'Falha ao carregar dados do grupo.';
        },
      );
    } finally {
      notifyListeners();
    }
  }

  /// Validates and submits the form, creating or updating a document group.
  ///
  /// Sets [status] to [DocumentGroupFormStatus.saved] on success so the UI can
  /// navigate away. Sets [status] to [DocumentGroupFormStatus.error] on failure.
  Future<void> save() async {
    _status = DocumentGroupFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final result = _id.isEmpty
          ? await _documentGroupRepository.createDocumentGroup(
              companyId,
              name: nameController.text.trim(),
              description: descriptionController.text.trim(),
            )
          : await _documentGroupRepository.updateDocumentGroup(
              companyId,
              id: _id,
              name: nameController.text.trim(),
              description: descriptionController.text.trim(),
            );

      result.fold(
        onSuccess: (_) => _status = DocumentGroupFormStatus.saved,
        onError: (_) {
          _status = DocumentGroupFormStatus.error;
          _errorMessage =
              'Falha ao salvar grupo. Verifique os dados e tente novamente.';
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
    super.dispose();
  }
}
