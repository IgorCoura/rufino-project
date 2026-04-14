import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../../../core/utils/document_scanner_service.dart';
import '../../../../data/models/document_range_item.dart';
import '../../../../domain/entities/address.dart';
import '../../../../domain/entities/employee.dart';
import '../../../../domain/entities/department.dart';
import '../../../../domain/entities/document_group_with_documents.dart';
import '../../../../domain/entities/employee_contact.dart';
import '../../../../domain/entities/employee_document.dart';
import '../../../../domain/entities/employee_contract.dart';
import '../../../../domain/entities/employee_dependent.dart';
import '../../../../domain/entities/selection_option.dart';
import '../../../../domain/entities/employee_id_card.dart';
import '../../../../domain/entities/employee_personal_info.dart';
import '../../../../domain/entities/employee_profile.dart';
import '../../../../domain/entities/position.dart';
import '../../../../domain/entities/remuneration.dart';
import '../../../../domain/entities/workplace.dart';
import '../../../../domain/entities/role.dart';
import '../../../../domain/entities/employee_medical_exam.dart';
import '../../../../domain/entities/employee_military_document.dart';
import '../../../../domain/entities/employee_social_integration_program.dart';
import '../../../../domain/entities/employee_vote_id.dart';
import '../../../../domain/entities/personal_info_options.dart';
import '../../../../core/utils/error_messages.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/department_repository.dart';
import '../../../../domain/repositories/document_group_repository.dart';
import '../../../../domain/repositories/employee_repository.dart';
import '../../../../domain/repositories/workplace_repository.dart';

/// Tracks a selected document unit for batch operations.
class SelectedDocumentUnit {
  const SelectedDocumentUnit({
    required this.documentId,
    required this.documentUnitId,
    required this.documentName,
    required this.documentUnitDate,
    required this.canGenerate,
    required this.hasFile,
  });

  /// The parent document id.
  final String documentId;

  /// The unit id.
  final String documentUnitId;

  /// The parent document name (for UI display).
  final String documentName;

  /// The unit date string (for UI display).
  final String documentUnitDate;

  /// Whether a PDF can be generated for this unit.
  final bool canGenerate;

  /// Whether this unit has an attached file.
  final bool hasFile;
}

/// Possible states for the employee profile screen.
enum EmployeeProfileStatus { loading, idle, saving, error }

/// Possible load/save states for a lazily-loaded profile section.
enum SectionLoadStatus { notLoaded, loading, loaded, saving, error }

/// Manages state for the employee profile screen.
///
/// Loads the selected company context, the employee profile, the profile image,
/// and related role/workplace labels needed by the UI. The ViewModel also
/// handles inline name editing, avatar upload, and the "mark as inactive" action.
///
/// Additional sections (contact, address, personalInfo, idCard, voteId,
/// militaryDocument) are lazily loaded on first expansion to avoid unnecessary
/// API calls.
class EmployeeProfileViewModel extends ChangeNotifier {
  EmployeeProfileViewModel({
    required CompanyRepository companyRepository,
    required EmployeeRepository employeeRepository,
    required DepartmentRepository departmentRepository,
    required WorkplaceRepository workplaceRepository,
    required DocumentGroupRepository documentGroupRepository,
    DocumentScannerService? scannerService,
  })  : _companyRepository = companyRepository,
        _employeeRepository = employeeRepository,
        _departmentRepository = departmentRepository,
        _workplaceRepository = workplaceRepository,
        _documentGroupRepository = documentGroupRepository,
        _scannerService = scannerService;

  final CompanyRepository _companyRepository;
  final EmployeeRepository _employeeRepository;
  final DepartmentRepository _departmentRepository;
  final WorkplaceRepository _workplaceRepository;
  final DocumentGroupRepository _documentGroupRepository;
  final DocumentScannerService? _scannerService;

  EmployeeProfileStatus _status = EmployeeProfileStatus.idle;
  String? _companyId;
  EmployeeProfile? _profile;
  Uint8List? _imageBytes;
  String? _roleName;
  String? _workplaceName;
  String? _errorMessage;
  String? _snackMessage;
  List<String> _serverErrors = const [];

  /// Server-provided error messages extracted from the API response, if any.
  List<String> get serverErrors => _serverErrors;

  bool _isEditingName = false;
  String _pendingName = '';

  // ─── Contact section ──────────────────────────────────────────────────────

  SectionLoadStatus _contactStatus = SectionLoadStatus.notLoaded;
  EmployeeContact? _contact;

  // ─── Address section ──────────────────────────────────────────────────────

  SectionLoadStatus _addressStatus = SectionLoadStatus.notLoaded;
  Address? _address;

  // ─── Personal info section ────────────────────────────────────────────────

  SectionLoadStatus _personalInfoStatus = SectionLoadStatus.notLoaded;
  EmployeePersonalInfo? _personalInfo;
  PersonalInfoOptions? _personalInfoOptions;

  // ─── ID card section ──────────────────────────────────────────────────────

  SectionLoadStatus _idCardStatus = SectionLoadStatus.notLoaded;
  EmployeeIdCard? _idCard;

  // ─── Vote ID section ──────────────────────────────────────────────────────

  SectionLoadStatus _voteIdStatus = SectionLoadStatus.notLoaded;
  EmployeeVoteId? _voteId;

  // ─── Social Integration Program (PIS) section ─────────────────────────────

  SectionLoadStatus _socialIntegrationProgramStatus =
      SectionLoadStatus.notLoaded;
  EmployeeSocialIntegrationProgram? _socialIntegrationProgram;

  // ─── Military document section ────────────────────────────────────────────

  SectionLoadStatus _militaryDocumentStatus = SectionLoadStatus.notLoaded;
  EmployeeMilitaryDocument? _militaryDocument;

  // ─── Medical exam section ──────────────────────────────────────────────────

  SectionLoadStatus _medicalExamStatus = SectionLoadStatus.notLoaded;
  EmployeeMedicalExam? _medicalExam;

  // ─── Role info section ─────────────────────────────────────────────────────

  SectionLoadStatus _roleInfoStatus = SectionLoadStatus.notLoaded;
  List<Department> _allDepartments = [];
  List<PaymentUnit> _paymentUnits = [];
  List<SalaryType> _salaryTypes = [];
  String _currentDepartmentId = '';
  String _currentPositionId = '';

  // ─── Dependents section ────────────────────────────────────────────────────

  SectionLoadStatus _dependentsStatus = SectionLoadStatus.notLoaded;
  List<EmployeeDependent> _dependents = [];

  /// Dependency type options (static — "Filho(a)" / "Cônjuge").
  static const dependencyTypeOptions = [
    SelectionOption(id: '1', name: 'Filho(a)'),
    SelectionOption(id: '2', name: 'Cônjuge'),
  ];

  // ─── Workplace info section ────────────────────────────────────────────────

  SectionLoadStatus _workplaceInfoStatus = SectionLoadStatus.notLoaded;
  List<Workplace> _allWorkplaces = [];

  // ─── Documents section ──────────────────────────────────────────────────

  SectionLoadStatus _documentsStatus = SectionLoadStatus.notLoaded;
  List<DocumentGroupWithDocuments> _documentGroups = [];
  bool _isSelectingRange = false;
  List<SelectedDocumentUnit> _selectedDocumentUnits = [];
  final Map<String, int> _pageSizeMap = {};
  double _uploadProgress = 0.0;

  // ─── Document signing options section ─────────────────────────────────────

  SectionLoadStatus _signingOptionsStatus = SectionLoadStatus.notLoaded;
  List<SelectionOption> _signingOptions = [];

  // ─── Contracts section ──────────────────────────────────────────────────

  SectionLoadStatus _contractsStatus = SectionLoadStatus.notLoaded;
  List<EmployeeContractInfo> _contracts = [];
  List<SelectionOption> _contractTypes = [];

  // ─── Public getters — core profile ────────────────────────────────────────

  /// The current loading/saving/error state.
  EmployeeProfileStatus get status => _status;

  /// Whether the initial profile data is currently being fetched.
  bool get isLoading => _status == EmployeeProfileStatus.loading;

  /// Whether an update action is currently in progress.
  bool get isSaving => _status == EmployeeProfileStatus.saving;

  /// Whether the screen is currently in an error state.
  ///
  /// Returns `true` when the top-level status is error **or** when a section
  /// save produced server error messages that have not yet been consumed.
  bool get hasError =>
      _status == EmployeeProfileStatus.error || _serverErrors.isNotEmpty;

  /// The loaded employee profile, or null when not yet available.
  EmployeeProfile? get profile => _profile;

  /// The loaded profile image bytes, or null when no image is available.
  Uint8List? get imageBytes => _imageBytes;

  /// The resolved role display name, or null when unavailable.
  String? get roleName => _roleName;

  /// The resolved workplace display name, or null when unavailable.
  String? get workplaceName => _workplaceName;

  /// The last human-readable error message, when [hasError] is true.
  String? get errorMessage => _errorMessage;

  /// A transient success message for the UI to present once.
  String? get snackMessage => _snackMessage;

  /// Whether the name field is currently being edited.
  bool get isEditingName => _isEditingName;

  /// The draft name value while editing, or an empty string when not editing.
  String get pendingName => _pendingName;

  /// Returns the role label to display in the profile overview.
  String get roleLabel => _roleName?.trim().isNotEmpty == true
      ? _roleName!
      : 'Sem função atribuída';

  /// Returns the workplace label to display in the profile overview.
  String get workplaceLabel => _workplaceName?.trim().isNotEmpty == true
      ? _workplaceName!
      : 'Sem local de trabalho atribuído';

  // ─── Public getters — section contact ─────────────────────────────────────

  /// The current load status of the contact section.
  SectionLoadStatus get contactStatus => _contactStatus;

  /// The loaded contact, or null when not yet loaded.
  EmployeeContact? get contact => _contact;

  // ─── Public getters — section address ─────────────────────────────────────

  /// The current load status of the address section.
  SectionLoadStatus get addressStatus => _addressStatus;

  /// The loaded address, or null when not yet loaded.
  Address? get address => _address;

  // ─── Public getters — section personal info ───────────────────────────────

  /// The current load status of the personal info section.
  SectionLoadStatus get personalInfoStatus => _personalInfoStatus;

  /// The loaded personal info, or null when not yet loaded.
  EmployeePersonalInfo? get personalInfo => _personalInfo;

  /// The loaded personal info selection options, or null when not yet loaded.
  PersonalInfoOptions? get personalInfoOptions => _personalInfoOptions;

  // ─── Public getters — section ID card ─────────────────────────────────────

  /// The current load status of the ID card section.
  SectionLoadStatus get idCardStatus => _idCardStatus;

  /// The loaded ID card data, or null when not yet loaded.
  EmployeeIdCard? get idCard => _idCard;

  // ─── Public getters — section vote ID ─────────────────────────────────────

  /// The current load status of the vote ID section.
  SectionLoadStatus get voteIdStatus => _voteIdStatus;

  /// The loaded voter registration, or null when not yet loaded.
  EmployeeVoteId? get voteId => _voteId;

  // ─── Public getters — section social integration program ─────────────────

  /// The current load status of the PIS/PASEP section.
  SectionLoadStatus get socialIntegrationProgramStatus =>
      _socialIntegrationProgramStatus;

  /// The loaded PIS/PASEP registration, or null when not yet loaded.
  EmployeeSocialIntegrationProgram? get socialIntegrationProgram =>
      _socialIntegrationProgram;

  // ─── Public getters — section military document ────────────────────────────

  /// The current load status of the military document section.
  SectionLoadStatus get militaryDocumentStatus => _militaryDocumentStatus;

  /// The loaded military document, or null when not yet loaded.
  EmployeeMilitaryDocument? get militaryDocument => _militaryDocument;

  // ─── Public getters — section medical exam ────────────────────────────────

  /// The current load status of the medical exam section.
  SectionLoadStatus get medicalExamStatus => _medicalExamStatus;

  /// The loaded medical exam, or null when not yet loaded.
  EmployeeMedicalExam? get medicalExam => _medicalExam;

  // ─── Public getters — section role info ────────────────────────────────────

  /// The current load status of the role info section.
  SectionLoadStatus get roleInfoStatus => _roleInfoStatus;

  /// All departments with their positions and roles for the cascading dropdown.
  List<Department> get allDepartments => _allDepartments;

  /// The department id of the employee's current role.
  String get currentDepartmentId => _currentDepartmentId;

  /// The position id of the employee's current role.
  String get currentPositionId => _currentPositionId;

  /// Available payment unit options for salary display resolution.
  List<PaymentUnit> get paymentUnits => _paymentUnits;

  /// Available salary type options for salary display resolution.
  List<SalaryType> get salaryTypes => _salaryTypes;

  /// Finds the department, position, and role for the given [roleId] in the
  /// loaded department hierarchy.
  ({Department? department, Position? position, Role? role})
      findRoleInHierarchy(String roleId) {
    for (final dept in _allDepartments) {
      for (final pos in dept.positions) {
        for (final role in pos.roles) {
          if (role.id == roleId) {
            return (department: dept, position: pos, role: role);
          }
        }
      }
    }
    return (department: null, position: null, role: null);
  }

  /// Returns the positions available for [departmentId].
  List<Position> positionsForDepartment(String departmentId) {
    return _allDepartments
        .where((d) => d.id == departmentId)
        .firstOrNull
        ?.positions ?? [];
  }

  /// Returns the roles available for [positionId] within [departmentId].
  List<Role> rolesForPosition(String departmentId, String positionId) {
    return positionsForDepartment(departmentId)
        .where((p) => p.id == positionId)
        .firstOrNull
        ?.roles ?? [];
  }

  // ─── Public getters — section dependents ───────────────────────────────────

  /// The current load status of the dependents section.
  SectionLoadStatus get dependentsStatus => _dependentsStatus;

  /// The loaded list of dependents.
  List<EmployeeDependent> get dependents => _dependents;

  // ─── Public getters — section workplace info ───────────────────────────────

  /// The current load status of the workplace info section.
  SectionLoadStatus get workplaceInfoStatus => _workplaceInfoStatus;

  /// All workplaces available for selection.
  List<Workplace> get allWorkplaces => _allWorkplaces;

  // ─── Public getters — section contracts ────────────────────────────────────

  /// The current load status of the contracts section.
  SectionLoadStatus get contractsStatus => _contractsStatus;

  /// The loaded list of contracts.
  List<EmployeeContractInfo> get contracts => _contracts;

  /// The available contract type options for the new contract form.
  List<SelectionOption> get contractTypes => _contractTypes;

  // ─── Public getters — section signing options ──────────────────────────────

  /// The current load status of the document signing options section.
  SectionLoadStatus get signingOptionsStatus => _signingOptionsStatus;

  /// The available document signing options.
  List<SelectionOption> get signingOptions => _signingOptions;

  // ─── Public getters — section documents ────────────────────────────────────

  /// The current load status of the documents section.
  SectionLoadStatus get documentsStatus => _documentsStatus;

  /// The loaded document groups with their documents.
  List<DocumentGroupWithDocuments> get documentGroups => _documentGroups;

  /// Whether the user is in range selection mode.
  bool get isSelectingRange => _isSelectingRange;

  /// The currently selected document units for batch operations.
  UnmodifiableListView<SelectedDocumentUnit> get selectedDocumentUnits =>
      UnmodifiableListView(_selectedDocumentUnits);

  /// Whether [documentUnitId] is currently selected for batch operations.
  bool isDocumentUnitSelected(String documentUnitId) =>
      _selectedDocumentUnits
          .any((u) => u.documentUnitId == documentUnitId);

  /// Returns the configured page size for [documentId], defaulting to 10.
  int getPageSize(String documentId) => _pageSizeMap[documentId] ?? 10;

  /// The current upload progress as a value from 0.0 to 1.0.
  double get uploadProgress => _uploadProgress;

  /// Whether document scanning is available on the current platform.
  bool get isScanSupported =>
      _scannerService != null && _scannerService.isPlatformSupported;

  /// Launches the native document scanner and returns captured page images.
  ///
  /// Returns `null` if the user cancels or scanning is not supported.
  Future<List<Uint8List>?> scanPages() async {
    if (_scannerService == null) return null;
    return _scannerService.scanPages();
  }

  // ─── Core profile methods ──────────────────────────────────────────────────

  /// Loads the employee profile identified by [employeeId].
  Future<void> load(String employeeId) async {
    _status = EmployeeProfileStatus.loading;
    _errorMessage = null;
    _snackMessage = null;
    _isEditingName = false;
    _pendingName = '';
    // Reset all lazy sections so they reload for the new employee.
    _contactStatus = SectionLoadStatus.notLoaded;
    _contact = null;
    _addressStatus = SectionLoadStatus.notLoaded;
    _address = null;
    _personalInfoStatus = SectionLoadStatus.notLoaded;
    _personalInfo = null;
    _personalInfoOptions = null;
    _idCardStatus = SectionLoadStatus.notLoaded;
    _idCard = null;
    _voteIdStatus = SectionLoadStatus.notLoaded;
    _voteId = null;
    _socialIntegrationProgramStatus = SectionLoadStatus.notLoaded;
    _socialIntegrationProgram = null;
    notifyListeners();

    final companyResult = await _companyRepository.getSelectedCompany();
    _companyId = companyResult.valueOrNull?.id;

    if (_companyId == null) {
      _status = EmployeeProfileStatus.error;
      _errorMessage = 'Nenhuma empresa selecionada.';
      notifyListeners();
      return;
    }

    final profileResult =
        await _employeeRepository.getEmployeeProfile(_companyId!, employeeId);

    final loadedProfile = profileResult.valueOrNull;
    if (loadedProfile == null) {
      _status = EmployeeProfileStatus.error;
      _errorMessage = 'Não foi possível carregar o funcionário.';
      notifyListeners();
      return;
    }

    _profile = loadedProfile;
    _imageBytes = null;
    _roleName = null;
    _workplaceName = null;

    await _loadRelatedLabels();
    await _loadImage();

    _status = EmployeeProfileStatus.idle;
    notifyListeners();
  }

  /// Enters inline name editing mode, pre-filling the draft with the current name.
  void startEditingName() {
    _isEditingName = true;
    _pendingName = _profile?.name ?? '';
    notifyListeners();
  }

  /// Exits name editing mode without saving any changes.
  void cancelEditingName() {
    _isEditingName = false;
    _pendingName = '';
    notifyListeners();
  }

  /// Saves [newName] as the employee's display name.
  ///
  /// Sets [status] to [EmployeeProfileStatus.saving] while the request is in
  /// flight, then to [EmployeeProfileStatus.idle] on success or
  /// [EmployeeProfileStatus.error] on failure. Exits editing mode on success.
  Future<void> saveName(String newName) async {
    final currentProfile = _profile;
    final companyId = _companyId;
    final trimmed = newName.trim();
    if (currentProfile == null || companyId == null || trimmed.isEmpty) return;

    _status = EmployeeProfileStatus.saving;
    _errorMessage = null;
    notifyListeners();

    final result = await _employeeRepository.editEmployeeName(
      companyId,
      currentProfile.id,
      trimmed,
    );

    result.fold(
      onSuccess: (_) {
        _profile = currentProfile.copyWith(name: trimmed);
        _isEditingName = false;
        _pendingName = '';
        _snackMessage = 'Nome atualizado com sucesso.';
        _status = EmployeeProfileStatus.idle;
      },
      onError: (error) {
        _status = EmployeeProfileStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível atualizar o nome do funcionário.';
      },
    );

    notifyListeners();
  }

  /// Uploads [imageBytes] as the employee's new profile photo.
  ///
  /// [fileName] must include the file extension (e.g. `"photo.jpg"`).
  /// On success, refreshes the locally cached [imageBytes] with the uploaded data.
  Future<void> uploadAvatar(Uint8List imageBytes, String fileName) async {
    final currentProfile = _profile;
    final companyId = _companyId;
    if (currentProfile == null || companyId == null) return;

    if (imageBytes.length > 5 * 1024 * 1024) {
      _snackMessage = 'Imagem muito grande. Tamanho máximo: 5 MB.';
      notifyListeners();
      return;
    }

    _status = EmployeeProfileStatus.saving;
    _errorMessage = null;
    notifyListeners();

    final result = await _employeeRepository.uploadEmployeeImage(
      companyId,
      currentProfile.id,
      imageBytes,
      fileName,
    );

    result.fold(
      onSuccess: (_) {
        _imageBytes = imageBytes;
        _snackMessage = 'A foto do perfil foi atualizada com sucesso.';
        _status = EmployeeProfileStatus.idle;
      },
      onError: (error) {
        _status = EmployeeProfileStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível atualizar a foto do perfil.';
      },
    );

    notifyListeners();
  }

  /// Marks the current employee as inactive.
  Future<void> markAsInactive() async {
    final currentProfile = _profile;
    final companyId = _companyId;
    if (currentProfile == null ||
        companyId == null ||
        !currentProfile.canMarkAsInactive) {
      return;
    }

    _status = EmployeeProfileStatus.saving;
    _errorMessage = null;
    notifyListeners();

    final result = await _employeeRepository.markEmployeeAsInactive(
      companyId,
      currentProfile.id,
    );

    result.fold(
      onSuccess: (_) {
        _profile = currentProfile.copyWith(status: EmployeeStatus.inactive);
        _snackMessage = 'Funcionário marcado como inativo com sucesso.';
        _status = EmployeeProfileStatus.idle;
      },
      onError: (error) {
        _status = EmployeeProfileStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível atualizar o status do funcionário.';
      },
    );

    notifyListeners();
  }

  /// Clears the current transient snack message.
  void consumeSnackMessage() {
    _snackMessage = null;
  }

  /// Clears server error messages after they have been shown to the user.
  void consumeServerErrors() {
    _serverErrors = const [];
    _errorMessage = null;
  }

  // ─── Contact section methods ───────────────────────────────────────────────

  /// Lazily loads the employee contact section.
  ///
  /// Does nothing if the section has already been loaded or is currently loading.
  Future<void> loadContact() async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null ||
        currentProfile == null ||
        _contactStatus == SectionLoadStatus.loading ||
        _contactStatus == SectionLoadStatus.loaded) {
      return;
    }

    _contactStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result = await _employeeRepository.getEmployeeContact(
        companyId, currentProfile.id);

    result.fold(
      onSuccess: (data) {
        _contact = data;
        _contactStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _contactStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Saves updated contact information for the current employee.
  ///
  /// Shows a snack message on success.
  Future<void> saveContact(String cellphone, String email) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _contactStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editEmployeeContact(
      companyId,
      currentProfile.id,
      cellphone,
      email,
    );

    result.fold(
      onSuccess: (_) {
        _contact = EmployeeContact(cellphone: cellphone, email: email);
        _snackMessage = 'Contato atualizado com sucesso.';
        _contactStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _contactStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar o contato.';
      },
    );

    notifyListeners();
  }

  // ─── Address section methods ───────────────────────────────────────────────

  /// Lazily loads the employee address section.
  ///
  /// Does nothing if the section has already been loaded or is currently loading.
  Future<void> loadAddress() async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null ||
        currentProfile == null ||
        _addressStatus == SectionLoadStatus.loading ||
        _addressStatus == SectionLoadStatus.loaded) {
      return;
    }

    _addressStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result = await _employeeRepository.getEmployeeAddress(
        companyId, currentProfile.id);

    result.fold(
      onSuccess: (data) {
        _address = data;
        _addressStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _addressStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Saves updated address information for the current employee.
  ///
  /// Shows a snack message on success.
  Future<void> saveAddress(Address address) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _addressStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editEmployeeAddress(
      companyId,
      currentProfile.id,
      address,
    );

    result.fold(
      onSuccess: (_) {
        _address = address;
        _snackMessage = 'Endereço atualizado com sucesso.';
        _addressStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _addressStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar o endereço.';
      },
    );

    notifyListeners();
  }

  // ─── Personal info section methods ────────────────────────────────────────

  /// Lazily loads the employee personal info section and its selection options.
  ///
  /// Does nothing if the section has already been loaded or is currently loading.
  Future<void> loadPersonalInfo() async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null ||
        currentProfile == null ||
        _personalInfoStatus == SectionLoadStatus.loading ||
        _personalInfoStatus == SectionLoadStatus.loaded) {
      return;
    }

    _personalInfoStatus = SectionLoadStatus.loading;
    notifyListeners();

    final infoResult = await _employeeRepository.getEmployeePersonalInfo(
        companyId, currentProfile.id);
    final optionsResult =
        await _employeeRepository.getPersonalInfoOptions(companyId);

    bool hasError = false;

    infoResult.fold(
      onSuccess: (data) => _personalInfo = data,
      onError: (_) => hasError = true,
    );

    optionsResult.fold(
      onSuccess: (data) => _personalInfoOptions = data,
      onError: (_) => hasError = true,
    );

    _personalInfoStatus =
        hasError ? SectionLoadStatus.error : SectionLoadStatus.loaded;
    notifyListeners();
  }

  /// Saves updated personal info for the current employee.
  ///
  /// Shows a snack message on success.
  Future<void> savePersonalInfo(EmployeePersonalInfo personalInfo) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _personalInfoStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editEmployeePersonalInfo(
      companyId,
      currentProfile.id,
      personalInfo,
    );

    result.fold(
      onSuccess: (_) {
        _personalInfo = personalInfo;
        _snackMessage = 'Informações pessoais atualizadas com sucesso.';
        _personalInfoStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _personalInfoStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar as informações pessoais.';
      },
    );

    notifyListeners();
  }

  // ─── ID card section methods ───────────────────────────────────────────────

  /// Lazily loads the employee ID card (Identidade) section.
  ///
  /// Does nothing if the section has already been loaded or is currently loading.
  Future<void> loadIdCard() async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null ||
        currentProfile == null ||
        _idCardStatus == SectionLoadStatus.loading ||
        _idCardStatus == SectionLoadStatus.loaded) {
      return;
    }

    _idCardStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result =
        await _employeeRepository.getEmployeeIdCard(companyId, currentProfile.id);

    result.fold(
      onSuccess: (data) {
        _idCard = data;
        _idCardStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _idCardStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Saves updated ID card (Identidade) information for the current employee.
  ///
  /// Shows a snack message on success.
  Future<void> saveIdCard(EmployeeIdCard idCard) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _idCardStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editEmployeeIdCard(
      companyId,
      currentProfile.id,
      idCard,
    );

    result.fold(
      onSuccess: (_) {
        _idCard = idCard;
        _snackMessage = 'Dados do documento atualizados com sucesso.';
        _idCardStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _idCardStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar os dados do documento.';
      },
    );

    notifyListeners();
  }

  // ─── Vote ID section methods ───────────────────────────────────────────────

  /// Lazily loads the employee voter registration (Título de Eleitor) section.
  ///
  /// Does nothing if the section has already been loaded or is currently loading.
  Future<void> loadVoteId() async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null ||
        currentProfile == null ||
        _voteIdStatus == SectionLoadStatus.loading ||
        _voteIdStatus == SectionLoadStatus.loaded) {
      return;
    }

    _voteIdStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result = await _employeeRepository.getEmployeeVoteId(
        companyId, currentProfile.id);

    result.fold(
      onSuccess: (data) {
        _voteId = data;
        _voteIdStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _voteIdStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Saves updated voter registration (Título de Eleitor) for the current employee.
  ///
  /// Shows a snack message on success.
  Future<void> saveVoteId(String voteIdNumber) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _voteIdStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editEmployeeVoteId(
      companyId,
      currentProfile.id,
      voteIdNumber,
    );

    result.fold(
      onSuccess: (_) {
        _voteId = EmployeeVoteId(number: voteIdNumber);
        _snackMessage = 'Título de eleitor atualizado com sucesso.';
        _voteIdStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _voteIdStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar o título de eleitor.';
      },
    );

    notifyListeners();
  }

  // ─── Social Integration Program (PIS) section methods ───────────────────

  /// Lazily loads the employee PIS/PASEP section.
  ///
  /// Does nothing if the section has already been loaded or is currently loading.
  Future<void> loadSocialIntegrationProgram() async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null ||
        currentProfile == null ||
        _socialIntegrationProgramStatus == SectionLoadStatus.loading ||
        _socialIntegrationProgramStatus == SectionLoadStatus.loaded) {
      return;
    }

    _socialIntegrationProgramStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result = await _employeeRepository.getEmployeeSocialIntegrationProgram(
      companyId,
      currentProfile.id,
    );

    result.fold(
      onSuccess: (data) {
        _socialIntegrationProgram = data;
        _socialIntegrationProgramStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _socialIntegrationProgramStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Saves updated PIS/PASEP registration for the current employee.
  ///
  /// [socialIntegrationProgramNumber] must be the raw 11-digit value
  /// (without separators).
  Future<void> saveSocialIntegrationProgram(
    String socialIntegrationProgramNumber,
  ) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _socialIntegrationProgramStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result =
        await _employeeRepository.editEmployeeSocialIntegrationProgram(
      companyId,
      currentProfile.id,
      socialIntegrationProgramNumber,
    );

    result.fold(
      onSuccess: (_) {
        _socialIntegrationProgram = EmployeeSocialIntegrationProgram(
          number: socialIntegrationProgramNumber,
        );
        _snackMessage = 'PIS atualizado com sucesso.';
        _socialIntegrationProgramStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _socialIntegrationProgramStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar o PIS.';
      },
    );

    notifyListeners();
  }

  // ─── Military document section ────────────────────────────────────────────

  /// Loads the military document for the current employee on first expansion.
  Future<void> loadMilitaryDocument() async {
    if (_militaryDocumentStatus != SectionLoadStatus.notLoaded) return;
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _militaryDocumentStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result = await _employeeRepository.getMilitaryDocument(
        companyId, currentProfile.id);

    result.fold(
      onSuccess: (data) {
        _militaryDocument = data;
        _militaryDocumentStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _militaryDocumentStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Saves updated military document for the current employee.
  ///
  /// Shows a snack message on success.
  Future<void> saveMilitaryDocument(String number, String type) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _militaryDocumentStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editMilitaryDocument(
      companyId,
      currentProfile.id,
      number,
      type,
    );

    result.fold(
      onSuccess: (_) {
        _militaryDocument = _militaryDocument?.copyWith(
          number: number,
          type: type,
        );
        _snackMessage = 'Documento militar atualizado com sucesso.';
        _militaryDocumentStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _militaryDocumentStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar o documento militar.';
      },
    );

    notifyListeners();
  }

  // ─── Medical exam section ─────────────────────────────────────────────────

  /// Loads the medical admission exam for the current employee on first
  /// expansion.
  Future<void> loadMedicalExam() async {
    if (_medicalExamStatus != SectionLoadStatus.notLoaded) return;
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _medicalExamStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result =
        await _employeeRepository.getMedicalExam(companyId, currentProfile.id);

    result.fold(
      onSuccess: (data) {
        _medicalExam = data;
        _medicalExamStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _medicalExamStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Saves the updated medical admission exam for the current employee.
  ///
  /// [dateExam] and [validityExam] must be in `dd/MM/yyyy` display format.
  /// Shows a snack message on success.
  Future<void> saveMedicalExam(String dateExam, String validityExam) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _medicalExamStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editMedicalExam(
      companyId,
      currentProfile.id,
      dateExam,
      validityExam,
    );

    result.fold(
      onSuccess: (_) {
        _medicalExam = EmployeeMedicalExam(
          dateExam: dateExam,
          validityExam: validityExam,
        );
        _snackMessage = 'Exame médico admissional atualizado com sucesso.';
        _medicalExamStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _medicalExamStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar o exame médico.';
      },
    );

    notifyListeners();
  }

  // ─── Role info section ─────────────────────────────────────────────────────

  /// Loads the department hierarchy, payment units, salary types, and locates
  /// the employee's current role.
  Future<void> loadRoleInfo() async {
    if (_roleInfoStatus != SectionLoadStatus.notLoaded) return;
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _roleInfoStatus = SectionLoadStatus.loading;
    notifyListeners();

    final deptResult = await _departmentRepository.getDepartments(companyId);
    if (deptResult.isError) {
      _roleInfoStatus = SectionLoadStatus.error;
      notifyListeners();
      return;
    }
    _allDepartments = deptResult.valueOrNull ?? [];

    // Load lookup data for salary display. Non-critical — use empty lists
    // as fallback so the section still works even if lookups fail.
    final puResult =
        await _departmentRepository.getPaymentUnits(companyId);
    final stResult =
        await _departmentRepository.getSalaryTypes(companyId);
    _paymentUnits = puResult.valueOrNull ?? [];
    _salaryTypes = stResult.valueOrNull ?? [];
    _findCurrentRoleInHierarchy(currentProfile.roleId);
    _roleInfoStatus = SectionLoadStatus.loaded;

    notifyListeners();
  }

  /// Saves a new role assignment for the current employee.
  ///
  /// Updates the profile's roleId and refreshes the role label on success.
  Future<void> saveEmployeeRole(String roleId) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;
    if (roleId.isEmpty) return;

    _roleInfoStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editEmployeeRole(
      companyId,
      currentProfile.id,
      roleId,
    );

    result.fold(
      onSuccess: (_) {
        _profile = currentProfile.copyWith(roleId: roleId);
        _findCurrentRoleInHierarchy(roleId);
        // Update the header role label.
        _roleName = _allDepartments
            .expand((d) => d.positions)
            .expand((p) => p.roles)
            .where((r) => r.id == roleId)
            .map((r) => r.name)
            .firstOrNull;
        _snackMessage = 'Função atualizada com sucesso.';
        _roleInfoStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _roleInfoStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar a função.';
      },
    );

    notifyListeners();
  }

  /// Walks the department→position→role hierarchy to find [roleId].
  void _findCurrentRoleInHierarchy(String roleId) {
    _currentDepartmentId = '';
    _currentPositionId = '';
    for (final dept in _allDepartments) {
      for (final pos in dept.positions) {
        for (final role in pos.roles) {
          if (role.id == roleId) {
            _currentDepartmentId = dept.id;
            _currentPositionId = pos.id;
            return;
          }
        }
      }
    }
  }

  // ─── Workplace info section ─────────────────────────────────────────────

  /// Loads all workplaces and locates the employee's current workplace.
  Future<void> loadWorkplaceInfo() async {
    if (_workplaceInfoStatus != SectionLoadStatus.notLoaded) return;
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _workplaceInfoStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result = await _workplaceRepository.getWorkplaces(companyId);

    result.fold(
      onSuccess: (workplaces) {
        _allWorkplaces = workplaces;
        _workplaceInfoStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _workplaceInfoStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Saves a new workplace assignment for the current employee.
  ///
  /// Updates the profile's workplaceId and refreshes the workplace label on
  /// success.
  Future<void> saveEmployeeWorkplace(String workplaceId) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;
    if (workplaceId.isEmpty) return;

    _workplaceInfoStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editEmployeeWorkplace(
      companyId,
      currentProfile.id,
      workplaceId,
    );

    result.fold(
      onSuccess: (_) {
        _profile = currentProfile.copyWith(workplaceId: workplaceId);
        _workplaceName = _allWorkplaces
            .where((w) => w.id == workplaceId)
            .map((w) => w.name)
            .firstOrNull;
        _snackMessage = 'Local de trabalho atualizado com sucesso.';
        _workplaceInfoStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _workplaceInfoStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar o local de trabalho.';
      },
    );

    notifyListeners();
  }

  // ─── Documents section ──────────────────────────────────────────────────

  /// Loads the document groups with their documents on first expansion.
  Future<void> loadDocumentGroups() async {
    if (_documentsStatus != SectionLoadStatus.notLoaded) return;
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _documentsStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result = await _documentGroupRepository
        .getDocumentGroupsWithDocuments(companyId, currentProfile.id);

    result.fold(
      onSuccess: (data) {
        _documentGroups = data;
        _documentsStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _documentsStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Loads a specific document with paginated units and updates
  /// it inside its parent group.
  Future<void> loadDocumentUnits(
    String documentId, {
    int pageNumber = 1,
    int? pageSize,
    int? statusId,
  }) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    final effectivePageSize = pageSize ?? getPageSize(documentId);

    final result = await _employeeRepository.getDocumentById(
      companyId,
      currentProfile.id,
      documentId,
      pageNumber: pageNumber,
      pageSize: effectivePageSize,
      statusId: statusId,
    );

    result.fold(
      onSuccess: (doc) {
        _documentGroups = _documentGroups.map((group) {
          final index =
              group.documents.indexWhere((d) => d.id == documentId);
          if (index == -1) return group;
          final updatedDocs = List<EmployeeDocument>.from(group.documents);
          updatedDocs[index] = doc;
          return group.copyWithDocuments(updatedDocs);
        }).toList();
      },
      onError: (_) {},
    );

    notifyListeners();
  }

  /// Toggles range selection mode on or off.
  ///
  /// Clears the selection list when turning off.
  void toggleRangeSelectionMode() {
    _isSelectingRange = !_isSelectingRange;
    if (!_isSelectingRange) {
      _selectedDocumentUnits = [];
    }
    notifyListeners();
  }

  /// Adds or removes [unit] from the range selection.
  void toggleDocumentUnitSelection(SelectedDocumentUnit unit) {
    final index = _selectedDocumentUnits
        .indexWhere((u) => u.documentUnitId == unit.documentUnitId);
    if (index != -1) {
      _selectedDocumentUnits =
          List.from(_selectedDocumentUnits)..removeAt(index);
    } else {
      _selectedDocumentUnits =
          List.from(_selectedDocumentUnits)..add(unit);
    }
    notifyListeners();
  }

  /// Updates the page size for [documentId] and reloads units at page 1.
  Future<void> changePageSize(String documentId, int pageSize) async {
    _pageSizeMap[documentId] = pageSize;
    await loadDocumentUnits(documentId, pageNumber: 1, pageSize: pageSize);
  }

  /// Generates PDFs for the selected document units and returns ZIP bytes.
  ///
  /// Returns `null` if no units can be generated or the call fails.
  Future<Uint8List?> generateDocumentRange() async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return null;

    final canGenerate =
        _selectedDocumentUnits.where((u) => u.canGenerate).toList();
    if (canGenerate.isEmpty) {
      _snackMessage = 'Nenhum dos documentos selecionados pode ser gerado.';
      notifyListeners();
      return null;
    }

    final Map<String, List<String>> grouped = {};
    for (final item in canGenerate) {
      grouped.putIfAbsent(item.documentId, () => []);
      grouped[item.documentId]!.add(item.documentUnitId);
    }
    final items = grouped.entries
        .map((e) => DocumentRangeItem(
              documentId: e.key,
              documentUnitIds: e.value,
            ))
        .toList();

    final result = await _employeeRepository.generateDocumentRange(
      companyId,
      currentProfile.id,
      items,
    );

    Uint8List? bytes;
    result.fold(
      onSuccess: (data) {
        bytes = data;
        _snackMessage = 'Documentos gerados com sucesso!';
        _isSelectingRange = false;
        _selectedDocumentUnits = [];
      },
      onError: (_) {
        _snackMessage = 'Erro ao gerar documentos.';
      },
    );

    notifyListeners();
    return bytes;
  }

  /// Downloads files for the selected document units and returns ZIP bytes.
  ///
  /// Returns `null` if no units have files or the call fails.
  Future<Uint8List?> downloadDocumentRange() async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return null;

    final canDownload =
        _selectedDocumentUnits.where((u) => u.hasFile).toList();
    if (canDownload.isEmpty) {
      _snackMessage =
          'Nenhum dos documentos selecionados possui arquivo para download.';
      notifyListeners();
      return null;
    }

    final Map<String, List<String>> grouped = {};
    for (final item in canDownload) {
      grouped.putIfAbsent(item.documentId, () => []);
      grouped[item.documentId]!.add(item.documentUnitId);
    }
    final items = grouped.entries
        .map((e) => DocumentRangeItem(
              documentId: e.key,
              documentUnitIds: e.value,
            ))
        .toList();

    final result = await _employeeRepository.downloadDocumentRange(
      companyId,
      currentProfile.id,
      items,
    );

    Uint8List? bytes;
    result.fold(
      onSuccess: (data) {
        bytes = data;
        _snackMessage = 'Documentos baixados com sucesso!';
        _isSelectingRange = false;
        _selectedDocumentUnits = [];
      },
      onError: (_) {
        _snackMessage = 'Erro ao baixar documentos.';
      },
    );

    notifyListeners();
    return bytes;
  }

  /// Creates a new document unit and refreshes the document.
  Future<void> createDocumentUnit(String documentId) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    final result = await _employeeRepository.createDocumentUnit(
      companyId,
      currentProfile.id,
      documentId,
    );

    result.fold(
      onSuccess: (_) => _snackMessage = 'Documento criado com sucesso.',
      onError: (_) => _snackMessage = 'Erro ao criar documento.',
    );

    notifyListeners();
    await loadDocumentUnits(documentId);
  }

  /// Edits the date of a document unit and refreshes the document.
  Future<void> editDocumentUnitDate(
    String documentId,
    String documentUnitId,
    String date,
  ) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    final result = await _employeeRepository.editDocumentUnit(
      companyId,
      currentProfile.id,
      documentId,
      documentUnitId,
      date,
    );

    result.fold(
      onSuccess: (_) =>
          _snackMessage = 'Data do documento atualizada com sucesso.',
      onError: (_) => _snackMessage = 'Erro ao atualizar data.',
    );

    notifyListeners();
    await loadDocumentUnits(documentId);
  }

  /// Marks a document unit as not applicable and refreshes the document.
  Future<void> setDocumentUnitNotApplicable(
    String documentId,
    String documentUnitId,
  ) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    final result = await _employeeRepository.setDocumentUnitNotApplicable(
      companyId,
      currentProfile.id,
      documentId,
      documentUnitId,
    );

    result.fold(
      onSuccess: (_) =>
          _snackMessage = 'Documento marcado como não aplicável.',
      onError: (_) => _snackMessage = 'Erro ao marcar documento.',
    );

    notifyListeners();
    await loadDocumentUnits(documentId);
  }

  /// Generates a PDF for a document unit and returns the raw bytes.
  ///
  /// Returns null if the operation fails. Shows a snack message on error.
  Future<Uint8List?> generateDocument(
    String documentId,
    String documentUnitId,
  ) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return null;

    Uint8List? bytes;
    final result = await _employeeRepository.generateDocument(
      companyId,
      currentProfile.id,
      documentId,
      documentUnitId,
    );

    result.fold(
      onSuccess: (data) {
        bytes = data;
        _snackMessage = 'Documento gerado com sucesso.';
      },
      onError: (_) => _snackMessage = 'Erro ao gerar documento.',
    );

    notifyListeners();
    await loadDocumentUnits(documentId);
    return bytes;
  }

  /// Generates a document and sends it for digital signature.
  Future<void> generateAndSendToSign(
    String documentId,
    String documentUnitId,
    String dateLimitToSign,
    int reminderEveryNDays,
  ) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    final result = await _employeeRepository.generateAndSendToSign(
      companyId,
      currentProfile.id,
      documentId,
      documentUnitId,
      dateLimitToSign,
      reminderEveryNDays,
    );

    result.fold(
      onSuccess: (_) =>
          _snackMessage = 'Documento gerado e enviado para assinatura.',
      onError: (_) =>
          _snackMessage = 'Erro ao gerar e enviar para assinatura.',
    );

    notifyListeners();
    await loadDocumentUnits(documentId);
  }

  /// Downloads the file attached to a document unit and returns the bytes.
  ///
  /// Returns null if the operation fails.
  Future<Uint8List?> downloadDocumentUnit(
    String documentId,
    String documentUnitId,
  ) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return null;

    Uint8List? bytes;
    final result = await _employeeRepository.downloadDocumentUnit(
      companyId,
      currentProfile.id,
      documentId,
      documentUnitId,
    );

    result.fold(
      onSuccess: (data) => bytes = data,
      onError: (_) => _snackMessage = 'Erro ao baixar documento.',
    );

    notifyListeners();
    return bytes;
  }

  /// Uploads a file to a document unit and refreshes the document.
  ///
  /// Updates [uploadProgress] during the upload so the UI can show a
  /// determinate progress indicator.
  Future<void> uploadDocumentUnit(
    String documentId,
    String documentUnitId,
    Uint8List fileBytes,
    String fileName,
  ) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _uploadProgress = 0.0;

    final result = await _employeeRepository.uploadDocumentUnit(
      companyId,
      currentProfile.id,
      documentId,
      documentUnitId,
      fileBytes,
      fileName,
      onProgress: (progress) {
        _uploadProgress = progress;
        notifyListeners();
      },
    );

    _uploadProgress = 0.0;

    result.fold(
      onSuccess: (_) => _snackMessage = 'Arquivo enviado com sucesso.',
      onError: (_) => _snackMessage = 'Erro ao enviar arquivo.',
    );

    notifyListeners();
    await loadDocumentUnits(documentId);
  }

  /// Uploads a file and sends it for digital signature.
  ///
  /// Updates [uploadProgress] during the upload so the UI can show a
  /// determinate progress indicator.
  Future<void> uploadDocumentUnitToSign(
    String documentId,
    String documentUnitId,
    Uint8List fileBytes,
    String fileName,
    String dateLimitToSign,
    int reminderEveryNDays,
  ) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _uploadProgress = 0.0;

    final result = await _employeeRepository.uploadDocumentUnitToSign(
      companyId,
      currentProfile.id,
      documentId,
      documentUnitId,
      fileBytes,
      fileName,
      dateLimitToSign,
      reminderEveryNDays,
      onProgress: (progress) {
        _uploadProgress = progress;
        notifyListeners();
      },
    );

    _uploadProgress = 0.0;

    result.fold(
      onSuccess: (_) =>
          _snackMessage = 'Arquivo enviado para assinatura com sucesso.',
      onError: (_) =>
          _snackMessage = 'Erro ao enviar arquivo para assinatura.',
    );

    notifyListeners();
    await loadDocumentUnits(documentId);
  }

  // ─── Document signing options section ───────────────────────────────────

  /// Loads the available signing options on first expansion.
  Future<void> loadSigningOptions() async {
    if (_signingOptionsStatus != SectionLoadStatus.notLoaded) return;
    final companyId = _companyId;
    if (companyId == null) return;

    _signingOptionsStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result =
        await _employeeRepository.getDocumentSigningOptions(companyId);

    result.fold(
      onSuccess: (options) {
        _signingOptions = options;
        _signingOptionsStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _signingOptionsStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Saves the selected document signing option for the current employee.
  Future<void> saveSigningOption(String optionId) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _signingOptionsStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editDocumentSigningOptions(
      companyId,
      currentProfile.id,
      optionId,
    );

    result.fold(
      onSuccess: (_) {
        _profile =
            currentProfile.copyWith(documentSigningOptionsId: optionId);
        _snackMessage =
            'Opção de assinatura de documentos atualizada com sucesso.';
        _signingOptionsStatus = SectionLoadStatus.loaded;
      },
      onError: (error) {
        _signingOptionsStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar a opção de assinatura.';
      },
    );

    notifyListeners();
  }

  // ─── Contracts section ─────────────────────────────────────────────────

  /// Loads the contract history and contract type options for the current
  /// employee on first expansion.
  Future<void> loadContracts() async {
    if (_contractsStatus != SectionLoadStatus.notLoaded) return;
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _contractsStatus = SectionLoadStatus.loading;
    notifyListeners();

    final contractsResult =
        await _employeeRepository.getContracts(companyId, currentProfile.id);
    final typesResult =
        await _employeeRepository.getContractTypes(companyId);

    if (contractsResult.isError) {
      _contractsStatus = SectionLoadStatus.error;
      notifyListeners();
      return;
    }

    _contracts = contractsResult.valueOrNull ?? [];
    _contractTypes = typesResult.valueOrNull ?? [];
    _contractsStatus = SectionLoadStatus.loaded;
    notifyListeners();
  }

  /// Creates a new contract and reloads the list on success.
  Future<void> createContract(
    String initDate,
    String contractTypeId,
    String registration,
  ) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _contractsStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.createContract(
      companyId,
      currentProfile.id,
      initDate,
      contractTypeId,
      registration,
    );

    result.fold(
      onSuccess: (_) {
        _contractsStatus = SectionLoadStatus.notLoaded;
        _snackMessage = 'Contrato criado com sucesso.';
      },
      onError: (error) {
        _contractsStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível criar o contrato.';
      },
    );

    notifyListeners();

    if (_contractsStatus == SectionLoadStatus.notLoaded) {
      await loadContracts();
    }
  }

  /// Finishes the active contract and reloads the list on success.
  Future<void> finishContract(String finalDate) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _contractsStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.finishContract(
      companyId,
      currentProfile.id,
      finalDate,
    );

    result.fold(
      onSuccess: (_) {
        _contractsStatus = SectionLoadStatus.notLoaded;
        _snackMessage = 'Contrato finalizado com sucesso.';
      },
      onError: (error) {
        _contractsStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível finalizar o contrato.';
      },
    );

    notifyListeners();

    if (_contractsStatus == SectionLoadStatus.notLoaded) {
      await loadContracts();
    }
  }

  // ─── Dependents section ─────────────────────────────────────────────────

  /// Loads the list of dependents for the current employee on first expansion.
  ///
  /// Also ensures the personal info options (genders) are available since the
  /// dependent form needs them for the gender dropdown.
  Future<void> loadDependents() async {
    if (_dependentsStatus != SectionLoadStatus.notLoaded) return;
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _dependentsStatus = SectionLoadStatus.loading;
    notifyListeners();

    final result =
        await _employeeRepository.getDependents(companyId, currentProfile.id);

    // Load gender options if not yet available.
    if (_personalInfoOptions == null) {
      final optionsResult =
          await _employeeRepository.getPersonalInfoOptions(companyId);
      optionsResult.fold(
        onSuccess: (data) => _personalInfoOptions = data,
        onError: (_) {},
      );
    }

    result.fold(
      onSuccess: (data) {
        _dependents = data;
        _dependentsStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _dependentsStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  /// Creates a new dependent and reloads the list on success.
  Future<void> createDependent(EmployeeDependent dependent) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _dependentsStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.createDependent(
      companyId,
      currentProfile.id,
      dependent,
    );

    result.fold(
      onSuccess: (_) {
        // Reload the list so the server-assigned data is reflected.
        _dependentsStatus = SectionLoadStatus.notLoaded;
        _snackMessage = 'Dependente criado com sucesso.';
      },
      onError: (error) {
        _dependentsStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível salvar o dependente.';
      },
    );

    notifyListeners();

    // Reload if successful.
    if (_dependentsStatus == SectionLoadStatus.notLoaded) {
      await loadDependents();
    }
  }

  /// Updates an existing dependent and reloads the list on success.
  Future<void> editDependentData(EmployeeDependent dependent) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _dependentsStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.editDependent(
      companyId,
      currentProfile.id,
      dependent,
    );

    result.fold(
      onSuccess: (_) {
        _dependentsStatus = SectionLoadStatus.notLoaded;
        _snackMessage = 'Dependente atualizado com sucesso.';
      },
      onError: (error) {
        _dependentsStatus = SectionLoadStatus.error;
        _serverErrors = extractServerMessages(error);
        _errorMessage = _serverErrors.isNotEmpty
            ? _serverErrors.join('\n')
            : 'Não foi possível atualizar o dependente.';
      },
    );

    notifyListeners();

    if (_dependentsStatus == SectionLoadStatus.notLoaded) {
      await loadDependents();
    }
  }

  /// Removes the dependent identified by [dependentName].
  Future<void> removeDependent(String dependentName) async {
    final companyId = _companyId;
    final currentProfile = _profile;
    if (companyId == null || currentProfile == null) return;

    _dependentsStatus = SectionLoadStatus.saving;
    notifyListeners();

    final result = await _employeeRepository.removeDependent(
      companyId,
      currentProfile.id,
      dependentName,
    );

    result.fold(
      onSuccess: (_) {
        _dependents =
            _dependents.where((d) => d.originalName != dependentName).toList();
        _snackMessage = 'Dependente removido com sucesso.';
        _dependentsStatus = SectionLoadStatus.loaded;
      },
      onError: (_) {
        _dependentsStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
  }

  // ─── Validators — delegated to domain entities ──────────────────────────

  // ── Contact validators ──────────────────────────────────────────────────

  /// Delegates to [EmployeeContact.validatePhone].
  String? validatePhone(String? value) => EmployeeContact.validatePhone(value);

  /// Delegates to [EmployeeContact.validateEmail].
  String? validateEmail(String? value) => EmployeeContact.validateEmail(value);

  // ── Address validators ──────────────────────────────────────────────────

  /// Delegates to [Address.validateCep].
  String? validateCep(String? value) => Address.validateCep(value);

  /// Delegates to [Address.validateRequired].
  String? validateRequired(String? value, String label) =>
      Address.validateRequired(value, label);

  /// Delegates to [Address.validateState].
  String? validateAddressState(String? value) => Address.validateState(value);

  // ── ID card validators ──────────────────────────────────────────────────

  /// Delegates to [EmployeeIdCard.isCpfValid].
  bool isCpfValid(String cpf) => EmployeeIdCard.isCpfValid(cpf);

  /// Delegates to [EmployeeIdCard.validateCpf].
  String? validateCpf(String? value) => EmployeeIdCard.validateCpf(value);

  /// Delegates to [EmployeeIdCard.validateDateOfBirth].
  String? validateDateOfBirth(String? value) =>
      EmployeeIdCard.validateDateOfBirth(value);

  /// Delegates to [EmployeeIdCard.validateMotherName].
  String? validateMotherName(String? value) =>
      EmployeeIdCard.validateMotherName(value);

  /// Delegates to [EmployeeIdCard.validateFatherName].
  String? validateFatherName(String? value) =>
      EmployeeIdCard.validateFatherName(value);

  /// Delegates to [EmployeeIdCard.validateBirthCity].
  String? validateBirthCity(String? value) =>
      EmployeeIdCard.validateBirthCity(value);

  /// Delegates to [EmployeeIdCard.validateBirthState].
  String? validateBirthState(String? value) =>
      EmployeeIdCard.validateBirthState(value);

  /// Delegates to [EmployeeIdCard.validateNationality].
  String? validateNationality(String? value) =>
      EmployeeIdCard.validateNationality(value);

  // ── Vote ID validators ──────────────────────────────────────────────────

  /// Delegates to [EmployeeVoteId.isVoteIdValid].
  bool isVoteIdValid(String number) => EmployeeVoteId.isVoteIdValid(number);

  /// Delegates to [EmployeeVoteId.validateNumber].
  String? validateVoteIdNumber(String? value) =>
      EmployeeVoteId.validateNumber(value);

  // ── Social Integration Program validators ──────────────────────────────

  /// Delegates to [EmployeeSocialIntegrationProgram.isPisValid].
  bool isSocialIntegrationProgramValid(String number) =>
      EmployeeSocialIntegrationProgram.isPisValid(number);

  /// Delegates to [EmployeeSocialIntegrationProgram.validateNumber].
  String? validateSocialIntegrationProgramNumber(String? value) =>
      EmployeeSocialIntegrationProgram.validateNumber(value);

  // ── Military document validators ────────────────────────────────────────

  /// Delegates to [EmployeeMilitaryDocument.validateNumber].
  String? validateMilitaryNumber(String? value) =>
      EmployeeMilitaryDocument.validateNumber(value);

  /// Delegates to [EmployeeMilitaryDocument.validateType].
  String? validateMilitaryType(String? value) =>
      EmployeeMilitaryDocument.validateType(value);

  // ── Medical exam validators ─────────────────────────────────────────────

  /// Delegates to [EmployeeMedicalExam.validateDateExam].
  String? validateDateExam(String? value) =>
      EmployeeMedicalExam.validateDateExam(value);

  /// Delegates to [EmployeeMedicalExam.validateExamValidity].
  String? validateExamValidity(String? value) =>
      EmployeeMedicalExam.validateExamValidity(value);

  // ── Contract validators ──────────────────────────────────────────────────

  /// Delegates to [EmployeeContractInfo.validateInitDate].
  String? validateContractInitDate(String? value) =>
      EmployeeContractInfo.validateInitDate(value);

  /// Delegates to [EmployeeContractInfo.validateFinalDate].
  String? validateContractFinalDate(String? value) =>
      EmployeeContractInfo.validateFinalDate(value);

  // ── Document unit validators ────────────────────────────────────────────

  /// Delegates to [DocumentUnit.validateDate].
  String? validateDocumentUnitDate(String? value) =>
      DocumentUnit.validateDate(value);

  // ── Dependent validators ─────────────────────────────────────────────────

  /// Delegates to [EmployeeDependent.validateName].
  String? validateDependentName(String? value) =>
      EmployeeDependent.validateName(value);

  // ── Role info helpers ───────────────────────────────────────────────────

  /// Formats the salary info from a [Role] for display.
  ///
  /// Resolves payment unit and salary type names from the lookup data,
  /// since the department hierarchy stores only IDs.
  String formatSalary(Role role) {
    final rem = role.remuneration;
    final value = rem.baseSalary.value;
    if (value.isEmpty) return 'Não informado';

    final typeId = rem.baseSalary.type.id;
    var typeName = rem.baseSalary.type.name;
    if (typeName.isEmpty) {
      typeName = _salaryTypes
              .where((s) => s.id == typeId)
              .firstOrNull
              ?.name ??
          '';
    }

    final unitId = rem.paymentUnit.id;
    var unitName = rem.paymentUnit.name;
    if (unitName.isEmpty) {
      unitName = _paymentUnits
              .where((p) => p.id == unitId)
              .firstOrNull
              ?.name ??
          '';
    }

    final parts = <String>[
      if (typeName.isNotEmpty) typeName,
      '\$$value',
      if (unitName.isNotEmpty) unitName,
    ];
    return parts.join(' ');
  }

  // ─── Private helpers ───────────────────────────────────────────────────────

  Future<void> _loadRelatedLabels() async {
    final currentProfile = _profile;
    final companyId = _companyId;
    if (currentProfile == null || companyId == null) {
      return;
    }

    if (currentProfile.roleId.isNotEmpty) {
      final roleResult = await _departmentRepository.getRoleById(
        companyId,
        currentProfile.roleId,
      );
      _roleName = roleResult.valueOrNull?.name;
    }

    if (currentProfile.workplaceId.isNotEmpty) {
      final workplaceResult = await _workplaceRepository.getWorkplaceById(
        companyId,
        currentProfile.workplaceId,
      );
      _workplaceName = workplaceResult.valueOrNull?.name;
    }
  }

  Future<void> _loadImage() async {
    final currentProfile = _profile;
    final companyId = _companyId;
    if (currentProfile == null || companyId == null) {
      return;
    }

    final imageResult = await _employeeRepository.getEmployeeImage(
      companyId,
      currentProfile.id,
    );
    _imageBytes = imageResult.valueOrNull;
  }
}
