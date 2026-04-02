import 'dart:collection';

import 'package:flutter/widgets.dart';

import '../../../../core/utils/error_messages.dart';
import '../../../../domain/entities/require_document.dart';
import '../../../../domain/entities/selection_option.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/require_document_repository.dart';

/// Possible statuses for the require document form screen.
enum RequireDocumentFormStatus { loading, idle, saving, saved, error }

/// Manages state for creating or editing a require document.
///
/// When [id] is non-empty, [loadRequireDocument] must be called after
/// construction to populate the form fields from the API.
class RequireDocumentFormViewModel extends ChangeNotifier {
  RequireDocumentFormViewModel({
    required CompanyRepository companyRepository,
    required RequireDocumentRepository requireDocumentRepository,
  })  : _companyRepository = companyRepository,
        _requireDocumentRepository = requireDocumentRepository;

  final CompanyRepository _companyRepository;
  final RequireDocumentRepository _requireDocumentRepository;

  // ─── TextEditingControllers owned by this ViewModel ────────────────────────

  /// Controller for the require document name field.
  final nameController = TextEditingController();

  /// Controller for the require document description field.
  final descriptionController = TextEditingController();

  // ─── State ─────────────────────────────────────────────────────────────────

  String _id = '';
  RequireDocumentFormStatus _status = RequireDocumentFormStatus.idle;
  String? _errorMessage;
  List<String> _serverErrors = const [];

  /// Server-provided error messages extracted from the API response, if any.
  List<String> get serverErrors => _serverErrors;

  // ─── Association state ─────────────────────────────────────────────────────

  String _selectedAssociationTypeId = '';
  List<String> _selectedAssociationIds = [];
  List<SelectionOption> _associationTypes = [];
  List<SelectionOption> _associations = [];

  // ─── Document templates state ──────────────────────────────────────────────

  List<SelectionOption> _availableDocumentTemplates = [];
  List<SelectionOption> _selectedDocumentTemplates = [];

  // ─── Events and statuses state ─────────────────────────────────────────────

  List<SelectionOption> _availableEvents = [];
  List<SelectionOption> _availableStatuses = [];
  List<_ListenEventEntry> _listenEvents = [];

  // ─── Getters ───────────────────────────────────────────────────────────────

  /// The id of the require document being edited, empty when creating a new one.
  String get id => _id;

  /// Current status of the form operation.
  RequireDocumentFormStatus get status => _status;

  /// Whether the form is currently loading existing data from the API.
  bool get isLoading => _status == RequireDocumentFormStatus.loading;

  /// Whether the form is currently submitting data to the API.
  bool get isSaving => _status == RequireDocumentFormStatus.saving;

  /// Whether this ViewModel is in create mode (no existing require document).
  bool get isNew => _id.isEmpty;

  /// Human-readable error message set when [status] is [RequireDocumentFormStatus.error].
  String? get errorMessage => _errorMessage;

  /// The currently selected association type id.
  String get selectedAssociationTypeId => _selectedAssociationTypeId;

  /// The currently selected association ids.
  List<String> get selectedAssociationIds => _selectedAssociationIds;

  /// Available association type options (Função, Local de Trabalho).
  List<SelectionOption> get associationTypes => _associationTypes;

  /// Available associations for the selected type.
  List<SelectionOption> get associations => _associations;

  /// Available document template options.
  List<SelectionOption> get availableDocumentTemplates =>
      _availableDocumentTemplates;

  /// Currently selected document templates.
  UnmodifiableListView<SelectionOption> get selectedDocumentTemplates =>
      UnmodifiableListView(_selectedDocumentTemplates);

  /// Available lifecycle event options.
  List<SelectionOption> get availableEvents => _availableEvents;

  /// Available employee status options.
  List<SelectionOption> get availableStatuses => _availableStatuses;

  /// The currently configured listen events with their statuses.
  UnmodifiableListView<ListenEventDisplay> get listenEvents =>
      UnmodifiableListView(_listenEvents.map((e) => ListenEventDisplay(
            eventId: e.eventId,
            eventName: e.eventName,
            statuses: UnmodifiableListView(e.statuses),
          )));

  // ─── Association type changes ──────────────────────────────────────────────

  /// Updates the selected association type, resets associations, and loads
  /// new association options from the API.
  Future<void> onAssociationTypeChanged(String? typeId) async {
    _selectedAssociationTypeId = typeId ?? '';
    _selectedAssociationIds = [];
    _associations = [];
    notifyListeners();

    if (_selectedAssociationTypeId.isEmpty) return;

    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id ?? '';

    final result = await _requireDocumentRepository.getAssociations(
        companyId, _selectedAssociationTypeId);
    result.fold(
      onSuccess: (data) => _associations = data,
      onError: (_) => _associations = [],
    );
    notifyListeners();
  }

  /// Toggles an association id in the selected list.
  ///
  /// Adds the [associationId] if not present, removes it if already selected.
  void toggleAssociation(String associationId) {
    if (_selectedAssociationIds.contains(associationId)) {
      _selectedAssociationIds = _selectedAssociationIds
          .where((id) => id != associationId)
          .toList();
    } else {
      _selectedAssociationIds = [..._selectedAssociationIds, associationId];
    }
    notifyListeners();
  }

  /// Removes an association by its [associationId] from the selected list.
  void removeAssociation(String associationId) {
    _selectedAssociationIds = _selectedAssociationIds
        .where((id) => id != associationId)
        .toList();
    notifyListeners();
  }

  // ─── Document templates management ─────────────────────────────────────────

  /// Adds a document template by its [templateId] to the selected list.
  void addDocumentTemplate(String templateId) {
    final template = _availableDocumentTemplates
        .where((t) => t.id == templateId)
        .firstOrNull;
    if (template == null) return;
    if (_selectedDocumentTemplates.any((t) => t.id == templateId)) return;
    _selectedDocumentTemplates = [..._selectedDocumentTemplates, template];
    notifyListeners();
  }

  /// Removes a document template by its [templateId] from the selected list.
  void removeDocumentTemplate(String templateId) {
    _selectedDocumentTemplates = _selectedDocumentTemplates
        .where((t) => t.id != templateId)
        .toList();
    notifyListeners();
  }

  // ─── Events management ─────────────────────────────────────────────────────

  /// Adds a new listen event by its [eventId].
  void addListenEvent(String eventId) {
    final event = _availableEvents
        .where((e) => e.id == eventId)
        .firstOrNull;
    if (event == null) return;
    final id = int.tryParse(eventId) ?? 0;
    if (_listenEvents.any((e) => e.eventId == id)) return;
    _listenEvents = [
      ..._listenEvents,
      _ListenEventEntry(eventId: id, eventName: event.name, statuses: []),
    ];
    notifyListeners();
  }

  /// Removes a listen event by its [eventId].
  void removeListenEvent(int eventId) {
    _listenEvents =
        _listenEvents.where((e) => e.eventId != eventId).toList();
    notifyListeners();
  }

  /// Adds a status to the listen event identified by [eventId].
  void addStatusToEvent(int eventId, String statusId) {
    final status = _availableStatuses
        .where((s) => s.id == statusId)
        .firstOrNull;
    if (status == null) return;

    final sid = int.tryParse(statusId) ?? 0;
    _listenEvents = _listenEvents.map((e) {
      if (e.eventId != eventId) return e;
      if (e.statuses.any((s) => s.id == sid)) return e;
      return _ListenEventEntry(
        eventId: e.eventId,
        eventName: e.eventName,
        statuses: [
          ...e.statuses,
          EventStatus(id: sid, name: status.name),
        ],
      );
    }).toList();
    notifyListeners();
  }

  /// Toggles a status on the listen event identified by [eventId].
  ///
  /// Adds the status if not present, removes it if already selected.
  void toggleStatusOnEvent(int eventId, String statusId) {
    final sid = int.tryParse(statusId) ?? 0;
    final event = _listenEvents.where((e) => e.eventId == eventId).firstOrNull;
    if (event == null) return;

    if (event.statuses.any((s) => s.id == sid)) {
      removeStatusFromEvent(eventId, sid);
    } else {
      addStatusToEvent(eventId, statusId);
    }
  }

  /// Removes a status from the listen event identified by [eventId].
  void removeStatusFromEvent(int eventId, int statusId) {
    _listenEvents = _listenEvents.map((e) {
      if (e.eventId != eventId) return e;
      return _ListenEventEntry(
        eventId: e.eventId,
        eventName: e.eventName,
        statuses: e.statuses.where((s) => s.id != statusId).toList(),
      );
    }).toList();
    notifyListeners();
  }

  // ─── Computed getters for UI filtering ──────────────────────────────────

  /// Returns associations that have not yet been selected.
  List<SelectionOption> get unselectedAssociations => _associations
      .where((a) => !_selectedAssociationIds.contains(a.id))
      .toList();

  /// Returns the selected associations as [SelectionOption] objects.
  List<SelectionOption> get selectedAssociations => _associations
      .where((a) => _selectedAssociationIds.contains(a.id))
      .toList();

  /// Returns document templates that have not yet been selected.
  List<SelectionOption> get unselectedDocumentTemplates =>
      _availableDocumentTemplates
          .where((t) => !_selectedDocumentTemplates.any((s) => s.id == t.id))
          .toList();

  /// Returns lifecycle events that have not yet been added.
  List<SelectionOption> get unselectedEvents => _availableEvents
      .where(
          (e) => !_listenEvents.any((le) => le.eventId.toString() == e.id))
      .toList();

  // ─── Validators — delegated to domain entity ──────────────────────────

  /// Delegates to [RequireDocument.validateName].
  String? validateName(String? v) => RequireDocument.validateName(v);

  /// Delegates to [RequireDocument.validateDescription].
  String? validateDescription(String? v) =>
      RequireDocument.validateDescription(v);

  // ─── Operations ────────────────────────────────────────────────────────────

  /// Loads an existing require document by [requireDocumentId] and populates
  /// the form controllers.
  Future<void> loadRequireDocument(String requireDocumentId) async {
    if (requireDocumentId.isEmpty) return;

    _id = requireDocumentId;
    _status = RequireDocumentFormStatus.loading;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      // Load lookup data.
      await _loadAllOptions(companyId);

      final result = await _requireDocumentRepository.getRequireDocumentById(
          companyId, requireDocumentId);
      result.fold(
        onSuccess: (doc) {
          nameController.text = doc.name;
          descriptionController.text = doc.description;
          _selectedAssociationTypeId = doc.associationTypeId.toString();
          _selectedAssociationIds = List<String>.from(doc.associationIds);

          _selectedDocumentTemplates = doc.documentTemplates
              .map((t) => SelectionOption(id: t.id, name: t.name))
              .toList();

          _listenEvents = doc.listenEvents
              .map((e) => _ListenEventEntry(
                    eventId: e.eventId,
                    eventName: e.eventName,
                    statuses: e.statuses,
                  ))
              .toList();

          _status = RequireDocumentFormStatus.idle;
        },
        onError: (_) {
          _status = RequireDocumentFormStatus.error;
          _errorMessage = 'Falha ao carregar dados do requerimento.';
        },
      );

      // Load associations for the selected type.
      if (_selectedAssociationTypeId.isNotEmpty) {
        final assocResult = await _requireDocumentRepository.getAssociations(
            companyId, _selectedAssociationTypeId);
        assocResult.fold(
          onSuccess: (data) => _associations = data,
          onError: (_) {},
        );
      }
    } finally {
      notifyListeners();
    }
  }

  /// Loads only the lookup data for new require document creation.
  Future<void> loadOptions() async {
    _status = RequireDocumentFormStatus.loading;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      await _loadAllOptions(companyId);

      _status = RequireDocumentFormStatus.idle;
    } catch (_) {
      _status = RequireDocumentFormStatus.error;
      _errorMessage = 'Falha ao carregar opções.';
    } finally {
      notifyListeners();
    }
  }

  /// Validates and submits the form, creating or updating a require document.
  ///
  /// Sets [status] to [RequireDocumentFormStatus.saved] on success so the UI
  /// can navigate away. Sets [status] to [RequireDocumentFormStatus.error] on
  /// failure.
  Future<void> save() async {
    _status = RequireDocumentFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    try {
      final companyResult = await _companyRepository.getSelectedCompany();
      final companyId = companyResult.valueOrNull?.id ?? '';

      final listenEventPayloads = _listenEvents
          .map((e) => ListenEventPayload(
                eventId: e.eventId,
                statusIds: e.statuses.map((s) => s.id).toList(),
              ))
          .toList();

      final result = _id.isEmpty
          ? await _requireDocumentRepository.createRequireDocument(
              companyId,
              name: nameController.text.trim(),
              description: descriptionController.text.trim(),
              associationIds: _selectedAssociationIds,
              associationTypeId:
                  int.tryParse(_selectedAssociationTypeId) ?? 0,
              documentTemplateIds:
                  _selectedDocumentTemplates.map((t) => t.id).toList(),
              listenEvents: listenEventPayloads,
            )
          : await _requireDocumentRepository.updateRequireDocument(
              companyId,
              id: _id,
              name: nameController.text.trim(),
              description: descriptionController.text.trim(),
              associationIds: _selectedAssociationIds,
              associationTypeId:
                  int.tryParse(_selectedAssociationTypeId) ?? 0,
              documentTemplateIds:
                  _selectedDocumentTemplates.map((t) => t.id).toList(),
              listenEvents: listenEventPayloads,
            );

      result.fold(
        onSuccess: (_) => _status = RequireDocumentFormStatus.saved,
        onError: (error) {
          _status = RequireDocumentFormStatus.error;
          _serverErrors = extractServerMessages(error);
          _errorMessage = _serverErrors.isNotEmpty
              ? _serverErrors.join('\n')
              : 'Falha ao salvar requerimento. Verifique os dados e tente novamente.';
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

  // ─── Private helpers ──────────────────────────────────────────────────────

  Future<void> _loadAllOptions(String companyId) async {
    final typesResult =
        await _requireDocumentRepository.getAssociationTypes(companyId);
    _associationTypes = typesResult.valueOrNull ?? [];

    final eventsResult =
        await _requireDocumentRepository.getEvents(companyId);
    _availableEvents = eventsResult.valueOrNull ?? [];

    final statusesResult =
        await _requireDocumentRepository.getStatuses(companyId);
    _availableStatuses = statusesResult.valueOrNull ?? [];

    final templatesResult =
        await _requireDocumentRepository.getDocumentTemplates(companyId);
    _availableDocumentTemplates = templatesResult.valueOrNull ?? [];
  }
}

/// Internal mutable representation of a listen event entry.
class _ListenEventEntry {
  const _ListenEventEntry({
    required this.eventId,
    required this.eventName,
    required this.statuses,
  });

  final int eventId;
  final String eventName;
  final List<EventStatus> statuses;
}

/// Immutable display representation of a listen event for the UI.
class ListenEventDisplay {
  const ListenEventDisplay({
    required this.eventId,
    required this.eventName,
    required this.statuses,
  });

  /// The event identifier.
  final int eventId;

  /// The translated display name of the event.
  final String eventName;

  /// The statuses associated with this event.
  final UnmodifiableListView<EventStatus> statuses;
}
