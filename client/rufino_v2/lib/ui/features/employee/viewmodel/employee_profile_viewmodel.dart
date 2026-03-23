import 'dart:typed_data';

import 'package:flutter/foundation.dart';

import '../../../../domain/entities/address.dart';
import '../../../../domain/entities/employee.dart';
import '../../../../domain/entities/department.dart';
import '../../../../domain/entities/employee_contact.dart';
import '../../../../domain/entities/employee_dependent.dart';
import '../../../../domain/entities/selection_option.dart';
import '../../../../domain/entities/employee_id_card.dart';
import '../../../../domain/entities/employee_personal_info.dart';
import '../../../../domain/entities/employee_profile.dart';
import '../../../../domain/entities/remuneration.dart';
import '../../../../domain/entities/workplace.dart';
import '../../../../domain/entities/role.dart';
import '../../../../domain/entities/employee_medical_exam.dart';
import '../../../../domain/entities/employee_military_document.dart';
import '../../../../domain/entities/employee_vote_id.dart';
import '../../../../domain/entities/personal_info_options.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/department_repository.dart';
import '../../../../domain/repositories/employee_repository.dart';
import '../../../../domain/repositories/workplace_repository.dart';

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
  })  : _companyRepository = companyRepository,
        _employeeRepository = employeeRepository,
        _departmentRepository = departmentRepository,
        _workplaceRepository = workplaceRepository;

  final CompanyRepository _companyRepository;
  final EmployeeRepository _employeeRepository;
  final DepartmentRepository _departmentRepository;
  final WorkplaceRepository _workplaceRepository;

  EmployeeProfileStatus _status = EmployeeProfileStatus.idle;
  String? _companyId;
  EmployeeProfile? _profile;
  Uint8List? _imageBytes;
  String? _roleName;
  String? _workplaceName;
  String? _errorMessage;
  String? _snackMessage;

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

  // ─── Public getters — core profile ────────────────────────────────────────

  /// The current loading/saving/error state.
  EmployeeProfileStatus get status => _status;

  /// Whether the initial profile data is currently being fetched.
  bool get isLoading => _status == EmployeeProfileStatus.loading;

  /// Whether an update action is currently in progress.
  bool get isSaving => _status == EmployeeProfileStatus.saving;

  /// Whether the screen is currently in an error state.
  bool get hasError => _status == EmployeeProfileStatus.error;

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
      onError: (_) {
        _status = EmployeeProfileStatus.error;
        _errorMessage = 'Não foi possível atualizar o nome do funcionário.';
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
      onError: (_) {
        _status = EmployeeProfileStatus.error;
        _errorMessage = 'Não foi possível atualizar a foto do perfil.';
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
      onError: (_) {
        _status = EmployeeProfileStatus.error;
        _errorMessage = 'Não foi possível atualizar o status do funcionário.';
      },
    );

    notifyListeners();
  }

  /// Clears the current transient snack message.
  void consumeSnackMessage() {
    _snackMessage = null;
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
      onError: (_) {
        _contactStatus = SectionLoadStatus.error;
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
      onError: (_) {
        _addressStatus = SectionLoadStatus.error;
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
      onError: (_) {
        _personalInfoStatus = SectionLoadStatus.error;
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
      onError: (_) {
        _idCardStatus = SectionLoadStatus.error;
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
      onError: (_) {
        _voteIdStatus = SectionLoadStatus.error;
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
      onError: (_) {
        _militaryDocumentStatus = SectionLoadStatus.error;
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
      onError: (_) {
        _medicalExamStatus = SectionLoadStatus.error;
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
      onError: (_) {
        _roleInfoStatus = SectionLoadStatus.error;
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
      onError: (_) {
        _workplaceInfoStatus = SectionLoadStatus.error;
      },
    );

    notifyListeners();
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
      onError: (_) {
        _dependentsStatus = SectionLoadStatus.error;
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
      onError: (_) {
        _dependentsStatus = SectionLoadStatus.error;
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

  // ─── Validators ──────────────────────────────────────────────────────────

  // ── Contact validators ──────────────────────────────────────────────────

  /// Validates a phone number.
  ///
  /// Optional field — returns null when empty. When filled, must have 10 or
  /// 11 digits.
  String? validatePhone(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    final digits = value.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 10 && digits.length != 11) {
      return 'Número inválido (ex: 11 98765-4321)';
    }
    return null;
  }

  /// Validates an email address.
  ///
  /// Optional field — returns null when empty. When filled, must match a
  /// basic email pattern.
  String? validateEmail(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    final emailRegex = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
    if (!emailRegex.hasMatch(value.trim())) return 'E-mail inválido';
    return null;
  }

  // ── Address validators ──────────────────────────────────────────────────

  /// Validates a Brazilian CEP (postal code).
  ///
  /// Required field with exactly 8 digits.
  String? validateCep(String? value) {
    if (value == null || value.trim().isEmpty) return 'CEP é obrigatório';
    final digits = value.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) return 'CEP inválido (ex: 01310-100)';
    return null;
  }

  /// Validates a required text field with a custom [label].
  String? validateRequired(String? value, String label) {
    if (value == null || value.trim().isEmpty) return '$label é obrigatório';
    return null;
  }

  /// Validates an optional state abbreviation field (for addresses).
  ///
  /// Returns null when empty. When filled, must be exactly 2 characters.
  String? validateAddressState(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    if (value.trim().length != 2) {
      return 'Use a sigla de 2 letras (ex: SP)';
    }
    return null;
  }

  // ── ID card validators ──────────────────────────────────────────────────

  /// Whether [cpf] passes the Brazilian CPF mathematical verification
  /// algorithm.
  bool isCpfValid(String cpf) {
    final digits = cpf.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 11) return false;
    if (RegExp(r'^(\d)\1{10}$').hasMatch(digits)) return false;

    int firstSum = 0;
    for (int i = 0; i < 9; i++) {
      firstSum += int.parse(digits[i]) * (10 - i);
    }
    final mod1 = firstSum % 11;
    final d1 = mod1 < 2 ? 0 : 11 - mod1;
    if (d1 != int.parse(digits[9])) return false;

    int secondSum = 0;
    for (int i = 0; i < 10; i++) {
      secondSum += int.parse(digits[i]) * (11 - i);
    }
    final mod2 = secondSum % 11;
    final d2 = mod2 < 2 ? 0 : 11 - mod2;
    return d2 == int.parse(digits[10]);
  }

  /// Validates a CPF field.
  String? validateCpf(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O CPF não pode ser vazio.';
    }
    if (value.trim().length > 100) {
      return 'O CPF não pode ser maior que 100 caracteres.';
    }
    if (!isCpfValid(value)) {
      return 'O CPF não é válido.';
    }
    return null;
  }

  /// Validates a date of birth in `dd/MM/yyyy` format.
  ///
  /// Must not be empty, must be a parseable date, not in the future, and not
  /// older than 100 years.
  String? validateDateOfBirth(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Data de nascimento não pode ser vazia.';
    }
    final digits = value.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) {
      return 'Data inválida (ex: 15/06/1990)';
    }
    try {
      final parts = value.split('/');
      final isoDate = '${parts[2]}-${parts[1]}-${parts[0]}';
      final date = DateTime.tryParse(isoDate);
      final now = DateTime.now();
      final hundredYearsAgo = now.subtract(const Duration(days: 36500));
      if (date == null ||
          date.isAfter(now) ||
          date.isBefore(hundredYearsAgo)) {
        return 'A Data de nascimento é inválida.';
      }
    } catch (_) {
      return 'A Data de nascimento é inválida.';
    }
    return null;
  }

  /// Validates the mother name field.
  String? validateMotherName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Nome da mãe não pode ser vazio.';
    }
    if (value.trim().length > 100) {
      return 'O Nome da mãe não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  /// Validates the father name field.
  String? validateFatherName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Nome do pai não pode ser vazio.';
    }
    if (value.trim().length > 100) {
      return 'O Nome do pai não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  /// Validates the birth city field.
  String? validateBirthCity(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Cidade de nascimento não pode ser vazia.';
    }
    if (value.trim().length > 100) {
      return 'A Cidade de nascimento não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  /// Validates a required state abbreviation (for ID card birth state).
  String? validateBirthState(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Estado de nascimento não pode ser vazio.';
    }
    if (value.trim().length != 2) {
      return 'Use a sigla de 2 letras (ex: SP)';
    }
    return null;
  }

  /// Validates the nationality field.
  String? validateNationality(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Nacionalidade não pode ser vazia.';
    }
    if (value.trim().length > 100) {
      return 'A Nacionalidade não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  // ── Vote ID validators ──────────────────────────────────────────────────

  /// Whether [number] passes the Brazilian voter registration (Título de
  /// Eleitor) mathematical verification algorithm.
  bool isVoteIdValid(String number) {
    final digits = number.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 12) return false;
    if (RegExp(r'^(\d)\1{11}$').hasMatch(digits)) return false;

    final uf = '${digits[8]}${digits[9]}';

    int sum = 0;
    const multiplierOne = [2, 3, 4, 5, 6, 7, 8, 9];
    for (int i = 0; i < 8; i++) {
      sum += int.parse(digits[i]) * multiplierOne[i];
    }
    int rest = sum % 11;
    if (rest > 9) {
      rest = 0;
    } else if (rest == 0 && (uf == '01' || uf == '02')) {
      rest = 1;
    }
    final z1 = rest.toString();

    sum = 0;
    const multiplierTwo = [7, 8, 9];
    final aux = '${digits[8]}${digits[9]}$z1';
    for (int i = 0; i < 3; i++) {
      sum += int.parse(aux[i]) * multiplierTwo[i];
    }
    rest = sum % 11;
    if (rest > 9) {
      rest = 0;
    } else if (rest == 0 && (uf == '01' || uf == '02')) {
      rest = 1;
    }
    final z2 = rest.toString();

    return digits.endsWith('$z1$z2');
  }

  /// Validates a voter registration number.
  String? validateVoteIdNumber(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.isEmpty) {
      return 'O Número do título não pode ser vazio.';
    }
    if (stripped.length != 12) {
      return 'Número inválido (ex: 0000.0000.0000)';
    }
    if (!isVoteIdValid(stripped)) {
      return 'O Número do título não é válido.';
    }
    return null;
  }

  // ── Military document validators ────────────────────────────────────────

  /// Validates the military document number field.
  String? validateMilitaryNumber(String? value) {
    final trimmed = (value ?? '').trim();
    if (trimmed.isEmpty) {
      return 'O Número do documento não pode ser vazio.';
    }
    if (trimmed.length > 20) {
      return 'O Número do documento não pode ter mais de 20 caracteres.';
    }
    return null;
  }

  /// Validates the military document type field.
  String? validateMilitaryType(String? value) {
    final trimmed = (value ?? '').trim();
    if (trimmed.isEmpty) {
      return 'O Tipo de documento não pode ser vazio.';
    }
    if (trimmed.length > 50) {
      return 'O Tipo de documento não pode ter mais de 50 caracteres.';
    }
    return null;
  }

  // ── Medical exam validators ─────────────────────────────────────────────

  /// Validates the exam date field.
  ///
  /// Required, must be a valid date within the last 365 days.
  String? validateDateExam(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.isEmpty) {
      return 'A Data do exame não pode ser vazia.';
    }
    if (stripped.length != 8) {
      return 'A Data do exame é inválida.';
    }
    try {
      final parts = value!.split('/');
      final date =
          DateTime.tryParse('${parts[2]}-${parts[1]}-${parts[0]}');
      if (date == null) return 'A Data do exame é inválida.';

      final now = DateTime.now();
      final minDate = now.subtract(const Duration(days: 365));
      final maxDate = now.add(const Duration(days: 1));
      if (date.isBefore(minDate) || date.isAfter(maxDate)) {
        return 'A Data do exame é inválida.';
      }
    } catch (_) {
      return 'A Data do exame é inválida.';
    }
    return null;
  }

  /// Validates the exam validity/expiry date field.
  ///
  /// Required, must be a future date (up to 10 years from now).
  String? validateExamValidity(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.isEmpty) {
      return 'A Validade do exame não pode ser vazia.';
    }
    if (stripped.length != 8) {
      return 'A Validade do exame é inválida.';
    }
    try {
      final parts = value!.split('/');
      final date =
          DateTime.tryParse('${parts[2]}-${parts[1]}-${parts[0]}');
      if (date == null) return 'A Validade do exame é inválida.';

      final now = DateTime.now();
      final minDate = now.add(const Duration(days: 1));
      final maxDate = now.add(const Duration(days: 3650));
      if (date.isBefore(minDate) || date.isAfter(maxDate)) {
        return 'A Validade do exame é inválida.';
      }
    } catch (_) {
      return 'A Validade do exame é inválida.';
    }
    return null;
  }

  // ── Dependent validators ─────────────────────────────────────────────────

  /// Validates the dependent name field.
  String? validateDependentName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Nome do dependente não pode ser vazio.';
    }
    if (value.trim().length > 100) {
      return 'O Nome não pode ter mais de 100 caracteres.';
    }
    return null;
  }

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
