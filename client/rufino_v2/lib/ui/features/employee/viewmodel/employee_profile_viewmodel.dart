import 'dart:typed_data';

import 'package:flutter/foundation.dart';

import '../../../../domain/entities/address.dart';
import '../../../../domain/entities/employee.dart';
import '../../../../domain/entities/employee_contact.dart';
import '../../../../domain/entities/employee_id_card.dart';
import '../../../../domain/entities/employee_personal_info.dart';
import '../../../../domain/entities/employee_profile.dart';
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
/// Additional sections (contact, address, personalInfo, idCard, voteId) are
/// lazily loaded on first expansion to avoid unnecessary API calls.
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
