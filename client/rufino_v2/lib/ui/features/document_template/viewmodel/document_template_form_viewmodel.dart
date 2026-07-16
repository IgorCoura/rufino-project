import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/widgets.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../core/utils/error_messages.dart';
import '../../../../domain/entities/document_template.dart';
import '../../../../domain/entities/selection_option.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/document_template_repository.dart';

/// Possible statuses for the document template form screen.
enum DocumentTemplateFormStatus { loading, idle, saving, saved, error }

/// Manages state for creating or editing a document template.
///
/// When [id] is non-empty, [loadTemplate] must be called after
/// construction to populate the form fields from the API.
class DocumentTemplateFormViewModel extends ChangeNotifier {
  DocumentTemplateFormViewModel({
    required CompanyRepository companyRepository,
    required DocumentTemplateRepository documentTemplateRepository,
  })  : _companyRepository = companyRepository,
        _documentTemplateRepository = documentTemplateRepository;

  final CompanyRepository _companyRepository;
  final DocumentTemplateRepository _documentTemplateRepository;

  // ─── TextEditingControllers owned by this ViewModel ────────────────────────

  /// Controller for the template name field.
  final nameController = TextEditingController();

  /// Controller for the template description field.
  final descriptionController = TextEditingController();

  /// Controller for the expiration rule's duration, in days.
  ///
  /// Only meaningful while [expirationEnabled] is true — the read-only field in
  /// the basic info section mirrors it through [validityDisplay].
  final validityController = TextEditingController();

  /// Controller for the workload rule's hours.
  ///
  /// Only meaningful while [workloadEnabled] is true.
  final workloadController = TextEditingController();

  /// Controller for the body file name.
  final bodyFileNameController = TextEditingController();

  /// Controller for the header file name.
  final headerFileNameController = TextEditingController();

  /// Controller for the footer file name.
  final footerFileNameController = TextEditingController();

  // ─── Input Formatters ─────────────────────────────────────────────────────

  /// Mask formatter for validity in days (up to 3 digits).
  final validityFormatter = MaskTextInputFormatter(
    mask: '###',
    filter: {'#': RegExp(r'[0-9]')},
  );

  /// Mask formatter for workload in hours (up to 3 digits).
  final workloadFormatter = MaskTextInputFormatter(
    mask: '###',
    filter: {'#': RegExp(r'[0-9]')},
  );

  // ─── State ─────────────────────────────────────────────────────────────────

  String _id = '';
  DocumentTemplateFormStatus _status = DocumentTemplateFormStatus.idle;
  String? _errorMessage;
  List<String> _serverErrors = const [];

  /// Server-provided error messages extracted from the API response, if any.
  List<String> get serverErrors => _serverErrors;
  bool _expirationEnabled = false;
  bool _workloadEnabled = false;
  bool _periodEnabled = false;
  PeriodGranularity? _selectedGranularity;
  bool _usePreviousPeriod = false;
  bool _acceptsSignature = false;
  String _selectedDocumentGroupId = '';
  List<int> _selectedRecoverDataTypeIds = [];
  List<SelectionOption> _documentGroups = [];
  List<SelectionOption> _recoverDataTypes = [];
  List<SelectionOption> _typeSignatures = [];
  List<PlaceSignatureData> _placeSignatures = [];
  bool _hasFile = false;
  String _recoverDataModels = '{}';

  /// The id of the template being edited, empty when creating a new one.
  String get id => _id;

  /// Current status of the form operation.
  DocumentTemplateFormStatus get status => _status;

  /// Whether the form is currently loading existing data from the API.
  bool get isLoading => _status == DocumentTemplateFormStatus.loading;

  /// Whether the form is currently submitting data to the API.
  bool get isSaving => _status == DocumentTemplateFormStatus.saving;

  /// Whether this ViewModel is in create mode (no existing template).
  bool get isNew => _id.isEmpty;

  /// Human-readable error message set when [status] is [DocumentTemplateFormStatus.error].
  String? get errorMessage => _errorMessage;

  /// Whether the expiration rule is active for this template.
  bool get expirationEnabled => _expirationEnabled;

  /// Whether the workload rule is active for this template.
  bool get workloadEnabled => _workloadEnabled;

  /// Whether the competência rule is active for this template.
  bool get periodEnabled => _periodEnabled;

  /// The competência granularity chosen in the form, or null when none.
  PeriodGranularity? get selectedGranularity => _selectedGranularity;

  /// The competência granularities the user can choose from.
  List<PeriodGranularity> get periodGranularities => PeriodGranularity.values;

  /// The rule set described by the form, ready to be persisted.
  ///
  /// A rule is only included when its switch is on and its input is valid,
  /// mirroring how the API models absence.
  TemplatePolicies get policies {
    final days =
        _expirationEnabled ? int.tryParse(validityController.text.trim()) : null;
    final hours =
        _workloadEnabled ? int.tryParse(workloadController.text.trim()) : null;

    return TemplatePolicies(
      expiration: days == null || days <= 0
          ? null
          : ExpirationRule(durationInDays: days),
      workload: hours == null || hours <= 0 ? null : WorkloadRule(hours: hours),
      period: _periodEnabled && _selectedGranularity != null
          ? PeriodRule(
              granularity: _selectedGranularity!,
              usePreviousPeriod: _usePreviousPeriod)
          : null,
    );
  }

  /// The validity to show in the read-only field of the basic info section.
  ///
  /// Empty when the rule is off — the legacy field is a mirror of the rule, not
  /// an input of its own.
  String get validityDisplay =>
      _expirationEnabled ? validityController.text.trim() : '';

  /// The workload to show in the read-only field of the basic info section.
  String get workloadDisplay =>
      _workloadEnabled ? workloadController.text.trim() : '';

  /// Whether the template uses the previous period as reference.
  bool get usePreviousPeriod => _usePreviousPeriod;

  /// Whether the generated document accepts a digital signature.
  bool get acceptsSignature => _acceptsSignature;

  /// The currently selected document group id.
  String get selectedDocumentGroupId => _selectedDocumentGroupId;

  /// Returns the selected document group id when it exists in [documentGroups],
  /// or null otherwise. Safe for use as [DropdownButtonFormField.value].
  String? get safeDocumentGroupId =>
      SelectionOption.safeId(_selectedDocumentGroupId, _documentGroups);

  /// The currently selected recover data type ids.
  List<int> get selectedRecoverDataTypeIds => _selectedRecoverDataTypeIds;

  /// Available document group options.
  List<SelectionOption> get documentGroups => _documentGroups;

  /// Available recover data type options.
  List<SelectionOption> get recoverDataTypes => _recoverDataTypes;

  /// Available type signature options (e.g. Assinatura, Visto).
  List<SelectionOption> get typeSignatures => _typeSignatures;

  /// The current list of signature placements.
  List<PlaceSignatureData> get placeSignatures => _placeSignatures;

  /// Whether the template has an uploaded file on the server.
  bool get hasFile => _hasFile;

  /// The loaded recover data models JSON string.
  String get recoverDataModels => _recoverDataModels;

  // ─── Setters ──────────────────────────────────────────────────────────────

  /// Turns the expiration rule on or off and notifies listeners.
  ///
  /// Turning it off clears the duration, so a stale value cannot survive a
  /// toggle and reappear on save.
  void setExpirationEnabled(bool value) {
    _expirationEnabled = value;
    if (!value) validityController.clear();
    notifyListeners();
  }

  /// Turns the workload rule on or off and notifies listeners.
  void setWorkloadEnabled(bool value) {
    _workloadEnabled = value;
    if (!value) workloadController.clear();
    notifyListeners();
  }

  /// Turns the competência rule on or off and notifies listeners.
  ///
  /// Turning it off clears the granularity and the retroactive flag, so a stale
  /// choice cannot survive a toggle and reappear on save.
  void setPeriodEnabled(bool value) {
    _periodEnabled = value;
    if (!value) {
      _selectedGranularity = null;
      _usePreviousPeriod = false;
    }
    notifyListeners();
  }

  /// Updates the competência granularity and notifies listeners.
  void setPeriodGranularity(PeriodGranularity? value) {
    _selectedGranularity = value;
    notifyListeners();
  }

  /// Updates the [usePreviousPeriod] flag and notifies listeners.
  void setUsePreviousPeriod(bool value) {
    _usePreviousPeriod = value;
    notifyListeners();
  }

  /// Turns signature acceptance on or off and notifies listeners.
  ///
  /// Turning it off clears the placements, so a document that no longer accepts
  /// signature cannot carry placements the API would reject (`PMD.DOCT10`).
  void setAcceptsSignature(bool value) {
    _acceptsSignature = value;
    if (!value) _placeSignatures = [];
    notifyListeners();
  }

  /// Updates the selected document group and notifies listeners.
  void setDocumentGroupId(String? id) {
    _selectedDocumentGroupId = id ?? '';
    notifyListeners();
  }

  /// Toggles a recover data type id in the selection (add or remove).
  void toggleRecoverDataType(int typeId) {
    if (_selectedRecoverDataTypeIds.contains(typeId)) {
      _selectedRecoverDataTypeIds =
          _selectedRecoverDataTypeIds.where((id) => id != typeId).toList();
    } else {
      _selectedRecoverDataTypeIds = [..._selectedRecoverDataTypeIds, typeId];
    }
    notifyListeners();
  }

  /// Replaces the recover data type at [index] with [newTypeId].
  void replaceRecoverDataType(int index, int newTypeId) {
    if (index < 0 || index >= _selectedRecoverDataTypeIds.length) return;
    _selectedRecoverDataTypeIds = List.of(_selectedRecoverDataTypeIds)
      ..[index] = newTypeId;
    notifyListeners();
  }

  /// Loads the recover data models JSON from the API.
  Future<void> loadRecoverDataModels() async {
    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id ?? '';
    if (companyId.isEmpty) return;

    final result =
        await _documentTemplateRepository.getRecoverDataModels(companyId);
    result.fold(
      onSuccess: (data) => _recoverDataModels = data,
      onError: (_, __) {},
    );
    notifyListeners();
  }

  // ─── Signature management ──────────────────────────────────────────────────

  /// Adds a new empty place signature entry.
  void addPlaceSignature() {
    _placeSignatures = [..._placeSignatures, const PlaceSignatureData()];
    notifyListeners();
  }

  /// Removes the place signature at [index].
  void removePlaceSignature(int index) {
    if (index < 0 || index >= _placeSignatures.length) return;
    _placeSignatures = List.of(_placeSignatures)..removeAt(index);
    notifyListeners();
  }

  /// Silently updates a field on the place signature at [index].
  ///
  /// Does NOT call [notifyListeners] to avoid rebuilding the form on every
  /// keystroke. Use [updatePlaceSignatureAndNotify] for dropdown changes that
  /// need an immediate UI refresh.
  void updatePlaceSignature(int index, PlaceSignatureData updated) {
    if (index < 0 || index >= _placeSignatures.length) return;
    _placeSignatures = List.of(_placeSignatures)..[index] = updated;
  }

  /// Updates a field on the place signature at [index] and rebuilds the UI.
  ///
  /// Use this for dropdown changes where the displayed value must update
  /// immediately.
  void updatePlaceSignatureAndNotify(int index, PlaceSignatureData updated) {
    if (index < 0 || index >= _placeSignatures.length) return;
    _placeSignatures = List.of(_placeSignatures)..[index] = updated;
    notifyListeners();
  }

  // ─── File management ──────────────────────────────────────────────────────

  /// Picks a file and uploads it to the template.
  ///
  /// Returns `true` when the upload succeeds, `false` when the user cancels
  /// the file picker or the upload fails. Maximum file size is 10 MB.
  Future<bool> pickAndUploadFile() async {
    if (_id.isEmpty) return false;

    final result = await FilePicker.platform.pickFiles(withData: true);
    if (result == null || result.files.isEmpty) return false;

    final file = result.files.first;
    if (file.bytes == null) return false;

    // 10 MB limit.
    if (file.bytes!.lengthInBytes > 10 * 1024 * 1024) {
      _errorMessage = 'O arquivo não pode ser maior que 10 MB.';
      _status = DocumentTemplateFormStatus.error;
      notifyListeners();
      return false;
    }

    _status = DocumentTemplateFormStatus.saving;
    notifyListeners();

    var success = false;
    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final uploadResult = await _documentTemplateRepository.uploadFile(
        companyId,
        _id,
        file.bytes!,
        file.name,
      );

      uploadResult.fold(
        onSuccess: (_) {
          _hasFile = true;
          _status = DocumentTemplateFormStatus.idle;
          success = true;
        },
        onError: (error, _) {
          _status = DocumentTemplateFormStatus.error;
          _serverErrors = extractServerMessages(error);
          _errorMessage = _serverErrors.isNotEmpty
              ? _serverErrors.join('\n')
              : 'Falha ao enviar arquivo.';
        },
      );
    } finally {
      notifyListeners();
    }
    return success;
  }

  /// Downloads the uploaded file for the template.
  ///
  /// Returns the raw bytes, or null on failure.
  Future<Uint8List?> downloadFile() async {
    if (_id.isEmpty) return null;

    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id ?? '';

    final result =
        await _documentTemplateRepository.downloadFile(companyId, _id);
    return result.valueOrNull;
  }

  // ─── Validators — delegated to domain entities ──────────────────────────

  /// Delegates to [DocumentTemplate.validateName].
  String? validateName(String? v) => DocumentTemplate.validateName(v);

  /// Delegates to [DocumentTemplate.validateDescription].
  String? validateDescription(String? v) =>
      DocumentTemplate.validateDescription(v);

  /// Delegates to [ExpirationRule.validateDuration].
  ///
  /// Only applies while the rule is on; an off rule has no field to validate.
  String? validateValidity(String? v) =>
      _expirationEnabled ? ExpirationRule.validateDuration(v) : null;

  /// Delegates to [WorkloadRule.validateHours].
  String? validateWorkload(String? v) =>
      _workloadEnabled ? WorkloadRule.validateHours(v) : null;

  /// Delegates to [PlaceSignatureData.validateField].
  String? validateSignatureNumber(String? v, String label) =>
      PlaceSignatureData.validateField(v, label);

  /// Delegates to [PlaceSignatureData.validateType].
  ///
  /// A placement without a type would be sent as `type: 0`, which the API
  /// rejects (there is no signature type 0) — so the type is mandatory.
  String? validateSignatureType(String? typeSignatureId) =>
      PlaceSignatureData.validateType(typeSignatureId);

  /// Delegates to [DocumentTemplate.validateFileName].
  String? validateFileName(String? v) => DocumentTemplate.validateFileName(v);

  // ─── Operations ────────────────────────────────────────────────────────────

  /// Loads an existing document template by [templateId] and populates the form controllers.
  Future<void> loadTemplate(String templateId) async {
    if (templateId.isEmpty) return;

    _id = templateId;
    _status = DocumentTemplateFormStatus.loading;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      // Load lookup data in parallel with the template.
      final groupsResult =
          await _documentTemplateRepository.getDocumentGroups(companyId);
      _documentGroups = groupsResult.valueOrNull ?? [];

      final typesResult =
          await _documentTemplateRepository.getRecoverDataTypes(companyId);
      _recoverDataTypes = typesResult.valueOrNull ?? [];

      final sigResult =
          await _documentTemplateRepository.getTypeSignatures(companyId);
      _typeSignatures = sigResult.valueOrNull ?? [];

      final result = await _documentTemplateRepository
          .getDocumentTemplateById(companyId, templateId);
      result.fold(
        onSuccess: (template) {
          nameController.text = template.name;
          descriptionController.text = template.description;
          _expirationEnabled = template.policies.expiration != null;
          _workloadEnabled = template.policies.workload != null;
          validityController.text =
              template.policies.expiration?.durationInDays.toString() ?? '';
          workloadController.text =
              template.policies.workload?.hours.toString() ?? '';
          final period = template.policies.period;
          _periodEnabled = period != null;
          _selectedGranularity = period?.granularity;
          _usePreviousPeriod = period?.usePreviousPeriod ?? false;
          _acceptsSignature = template.acceptsSignature;
          bodyFileNameController.text = template.bodyFileName;
          headerFileNameController.text = template.headerFileName;
          footerFileNameController.text = template.footerFileName;
          _selectedDocumentGroupId = template.documentGroupId;
          _selectedRecoverDataTypeIds = List.of(template.recoverDataTypeIds);
          _placeSignatures = List.of(template.placeSignatures);
          _status = DocumentTemplateFormStatus.idle;
        },
        onError: (_, __) {
          _status = DocumentTemplateFormStatus.error;
          _errorMessage = 'Falha ao carregar dados do template.';
        },
      );

      // Check if file exists on server (only when loaded successfully).
      if (_status == DocumentTemplateFormStatus.idle) {
        final hasFileResult =
            await _documentTemplateRepository.hasFile(companyId, templateId);
        _hasFile = hasFileResult.valueOrNull ?? false;
      }
    } finally {
      notifyListeners();
    }
  }

  /// Loads only the lookup data (groups and types) for new template creation.
  Future<void> loadOptions() async {
    _status = DocumentTemplateFormStatus.loading;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final groupsResult =
          await _documentTemplateRepository.getDocumentGroups(companyId);
      _documentGroups = groupsResult.valueOrNull ?? [];

      final typesResult =
          await _documentTemplateRepository.getRecoverDataTypes(companyId);
      _recoverDataTypes = typesResult.valueOrNull ?? [];

      final sigResult =
          await _documentTemplateRepository.getTypeSignatures(companyId);
      _typeSignatures = sigResult.valueOrNull ?? [];

      _status = DocumentTemplateFormStatus.idle;
    } catch (_) {
      _status = DocumentTemplateFormStatus.error;
      _errorMessage = 'Falha ao carregar opções.';
    } finally {
      notifyListeners();
    }
  }

  /// Validates and submits the form, creating or updating a document template.
  ///
  /// Sets [status] to [DocumentTemplateFormStatus.saved] on success so the UI can
  /// navigate away. Sets [status] to [DocumentTemplateFormStatus.error] on failure.
  Future<void> save() async {
    _status = DocumentTemplateFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final selectedPolicies = policies;

      final result = _id.isEmpty
          ? await _documentTemplateRepository.createDocumentTemplate(
              companyId,
              name: nameController.text.trim(),
              description: descriptionController.text.trim(),
              policies: selectedPolicies,
              acceptsSignature: _acceptsSignature,
              bodyFileName: bodyFileNameController.text.trim(),
              headerFileName: headerFileNameController.text.trim(),
              footerFileName: footerFileNameController.text.trim(),
              documentGroupId: _selectedDocumentGroupId,
              recoverDataTypeIds: _selectedRecoverDataTypeIds,
              placeSignatures: _placeSignatures,
            )
          : await _documentTemplateRepository.updateDocumentTemplate(
              companyId,
              id: _id,
              name: nameController.text.trim(),
              description: descriptionController.text.trim(),
              policies: selectedPolicies,
              acceptsSignature: _acceptsSignature,
              bodyFileName: bodyFileNameController.text.trim(),
              headerFileName: headerFileNameController.text.trim(),
              footerFileName: footerFileNameController.text.trim(),
              documentGroupId: _selectedDocumentGroupId,
              recoverDataTypeIds: _selectedRecoverDataTypeIds,
              placeSignatures: _placeSignatures,
            );

      result.fold(
        onSuccess: (_) => _status = DocumentTemplateFormStatus.saved,
        onError: (error, _) {
          _status = DocumentTemplateFormStatus.error;
          _serverErrors = extractServerMessages(error);
          _errorMessage = _serverErrors.isNotEmpty
              ? _serverErrors.join('\n')
              : 'Falha ao salvar template. Verifique os dados e tente novamente.';
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
    validityController.dispose();
    workloadController.dispose();
    bodyFileNameController.dispose();
    headerFileNameController.dispose();
    footerFileNameController.dispose();
    super.dispose();
  }
}
