import 'dart:typed_data';

import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/employee_contact.dart';
import 'package:rufino_v2/domain/entities/employee_id_card.dart';
import 'package:rufino_v2/domain/entities/employee_personal_info.dart';
import 'package:rufino_v2/domain/entities/employee_profile.dart';
import 'package:rufino_v2/domain/entities/employee_vote_id.dart';
import 'package:rufino_v2/domain/entities/personal_info_options.dart';
import 'package:rufino_v2/domain/entities/selection_option.dart';
import 'package:rufino_v2/domain/repositories/employee_repository.dart';

/// In-memory fake implementation of [EmployeeRepository] for tests.
///
/// All responses are configurable via setters before each test.
class FakeEmployeeRepository implements EmployeeRepository {
  // ─── Configurable return values ───────────────────────────────────────────

  List<Employee> _employees = [];
  EmployeeProfile? _employeeProfile;
  bool _shouldFail = false;
  String _createdId = 'new-employee-id';
  Uint8List? _imageBytes;

  EmployeeContact _contact = const EmployeeContact(
    cellphone: '11999990000',
    email: 'test@example.com',
  );

  Address _address = const Address(
    zipCode: '01310100',
    street: 'Av. Paulista',
    number: '1000',
    complement: '',
    neighborhood: 'Bela Vista',
    city: 'São Paulo',
    state: 'SP',
    country: 'Brasil',
  );

  EmployeePersonalInfo _personalInfo = const EmployeePersonalInfo(
    genderId: '1',
    maritalStatusId: '2',
    ethnicityId: '3',
    educationLevelId: '4',
    disabilityIds: [],
    disabilityObservation: '',
  );

  PersonalInfoOptions _personalInfoOptions = const PersonalInfoOptions(
    genders: [
      SelectionOption(id: '1', name: 'Homem'),
      SelectionOption(id: '2', name: 'Mulher'),
    ],
    maritalStatuses: [
      SelectionOption(id: '1', name: 'Solteiro(a)'),
      SelectionOption(id: '2', name: 'Casado(a)'),
    ],
    ethnicities: [
      SelectionOption(id: '1', name: 'Branco'),
      SelectionOption(id: '2', name: 'Negro'),
      SelectionOption(id: '3', name: 'Pardo'),
    ],
    educationLevels: [
      SelectionOption(id: '1', name: 'Ensino Médio Completo'),
      SelectionOption(id: '4', name: 'Ensino Superior Completo'),
    ],
    disabilities: [
      SelectionOption(id: '1', name: 'Física'),
      SelectionOption(id: '2', name: 'Visual'),
      SelectionOption(id: '3', name: 'Auditiva'),
    ],
  );

  EmployeeIdCard _idCard = const EmployeeIdCard(
    cpf: '111.444.777-35',
    motherName: 'Maria',
    fatherName: 'João',
    dateOfBirth: '01/01/1990',
    birthCity: 'São Paulo',
    birthState: 'SP',
    nationality: 'Brasileira',
  );

  EmployeeVoteId _voteId = const EmployeeVoteId(number: '1234.5678.0698');

  void setEmployees(List<Employee> employees) => _employees = employees;

  /// The profile returned by [getEmployeeProfile].
  void setEmployeeProfile(EmployeeProfile? profile) =>
      _employeeProfile = profile;

  /// When true every method returns [Result.error] with a generic exception.
  void setShouldFail(bool value) => _shouldFail = value;

  /// The id returned by [createEmployee].
  void setCreatedId(String id) => _createdId = id;

  /// The bytes returned by [getEmployeeImage] (null means no image).
  void setImageBytes(Uint8List? bytes) => _imageBytes = bytes;

  /// The contact returned by [getEmployeeContact].
  void setContact(EmployeeContact contact) => _contact = contact;

  /// The address returned by [getEmployeeAddress].
  void setAddress(Address address) => _address = address;

  /// The personal info returned by [getEmployeePersonalInfo].
  void setPersonalInfo(EmployeePersonalInfo info) => _personalInfo = info;

  /// The personal info options returned by [getPersonalInfoOptions].
  void setPersonalInfoOptions(PersonalInfoOptions options) =>
      _personalInfoOptions = options;

  /// The ID card returned by [getEmployeeIdCard].
  void setIdCard(EmployeeIdCard idCard) => _idCard = idCard;

  /// The vote ID returned by [getEmployeeVoteId].
  void setVoteId(EmployeeVoteId voteId) => _voteId = voteId;

  // ─── Captured call arguments (for assertion) ──────────────────────────────

  String? lastCreatedName;
  String? lastCreatedRoleId;
  String? lastCreatedWorkplaceId;
  String? lastProfileEmployeeId;
  int? lastGetEmployeesSkip;
  String? lastMarkInactiveEmployeeId;
  String? lastNameFilter;
  String? lastRoleFilter;
  String? lastUploadedFileName;
  String? lastEditedName;
  int getEmployeesCallCount = 0;

  String? lastSavedContactCellphone;
  String? lastSavedContactEmail;
  Address? lastSavedAddress;
  EmployeePersonalInfo? lastSavedPersonalInfo;
  EmployeeIdCard? lastSavedIdCard;
  String? lastSavedVoteIdNumber;

  // ─── Implementation ───────────────────────────────────────────────────────

  @override
  Future<Result<EmployeeProfile>> getEmployeeProfile(
    String companyId,
    String employeeId,
  ) async {
    lastProfileEmployeeId = employeeId;
    if (_shouldFail || _employeeProfile == null) {
      return Result.error(Exception('getEmployeeProfile failed'));
    }
    return Result.success(_employeeProfile!);
  }

  @override
  Future<Result<List<Employee>>> getEmployees(
    String companyId, {
    String? name,
    String? role,
    int? status,
    int? documentStatus,
    bool ascending = true,
    int pageSize = 15,
    int sizeSkip = 0,
  }) async {
    getEmployeesCallCount++;
    lastGetEmployeesSkip = sizeSkip;
    lastNameFilter = name;
    lastRoleFilter = role;
    if (_shouldFail) return Result.error(Exception('getEmployees failed'));
    return Result.success(_employees);
  }

  @override
  Future<Result<Uint8List?>> getEmployeeImage(
      String companyId, String employeeId) async {
    if (_shouldFail) return Result.error(Exception('getEmployeeImage failed'));
    return Result.success(_imageBytes);
  }

  @override
  Future<Result<void>> uploadEmployeeImage(
    String companyId,
    String employeeId,
    Uint8List imageBytes,
    String fileName,
  ) async {
    lastUploadedFileName = fileName;
    if (_shouldFail) {
      return Result.error(Exception('uploadEmployeeImage failed'));
    }
    _imageBytes = imageBytes;
    return const Result<void>.success(null);
  }

  @override
  Future<Result<void>> editEmployeeName(
    String companyId,
    String employeeId,
    String name,
  ) async {
    lastEditedName = name;
    if (_shouldFail) {
      return Result.error(Exception('editEmployeeName failed'));
    }
    if (_employeeProfile != null) {
      _employeeProfile = _employeeProfile!.copyWith(name: name);
    }
    return const Result<void>.success(null);
  }

  @override
  Future<Result<void>> markEmployeeAsInactive(
    String companyId,
    String employeeId,
  ) async {
    lastMarkInactiveEmployeeId = employeeId;
    if (_shouldFail) {
      return Result.error(Exception('markEmployeeAsInactive failed'));
    }
    if (_employeeProfile != null) {
      _employeeProfile = _employeeProfile!.copyWith(
        status: EmployeeStatus.inactive,
      );
    }
    return const Result<void>.success(null);
  }

  @override
  Future<Result<String>> createEmployee(
    String companyId, {
    required String name,
    required String roleId,
    required String workplaceId,
  }) async {
    if (_shouldFail) return Result.error(Exception('createEmployee failed'));
    lastCreatedName = name;
    lastCreatedRoleId = roleId;
    lastCreatedWorkplaceId = workplaceId;
    return Result.success(_createdId);
  }

  @override
  Future<Result<EmployeeContact>> getEmployeeContact(
    String companyId,
    String employeeId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getEmployeeContact failed'));
    }
    return Result.success(_contact);
  }

  @override
  Future<Result<void>> editEmployeeContact(
    String companyId,
    String employeeId,
    String cellphone,
    String email,
  ) async {
    lastSavedContactCellphone = cellphone;
    lastSavedContactEmail = email;
    if (_shouldFail) {
      return Result.error(Exception('editEmployeeContact failed'));
    }
    _contact = EmployeeContact(cellphone: cellphone, email: email);
    return const Result<void>.success(null);
  }

  @override
  Future<Result<Address>> getEmployeeAddress(
    String companyId,
    String employeeId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getEmployeeAddress failed'));
    }
    return Result.success(_address);
  }

  @override
  Future<Result<void>> editEmployeeAddress(
    String companyId,
    String employeeId,
    Address address,
  ) async {
    lastSavedAddress = address;
    if (_shouldFail) {
      return Result.error(Exception('editEmployeeAddress failed'));
    }
    _address = address;
    return const Result<void>.success(null);
  }

  @override
  Future<Result<EmployeePersonalInfo>> getEmployeePersonalInfo(
    String companyId,
    String employeeId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getEmployeePersonalInfo failed'));
    }
    return Result.success(_personalInfo);
  }

  @override
  Future<Result<PersonalInfoOptions>> getPersonalInfoOptions(
    String companyId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getPersonalInfoOptions failed'));
    }
    return Result.success(_personalInfoOptions);
  }

  @override
  Future<Result<void>> editEmployeePersonalInfo(
    String companyId,
    String employeeId,
    EmployeePersonalInfo personalInfo,
  ) async {
    lastSavedPersonalInfo = personalInfo;
    if (_shouldFail) {
      return Result.error(Exception('editEmployeePersonalInfo failed'));
    }
    _personalInfo = personalInfo;
    return const Result<void>.success(null);
  }

  @override
  Future<Result<EmployeeIdCard>> getEmployeeIdCard(
    String companyId,
    String employeeId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getEmployeeIdCard failed'));
    }
    return Result.success(_idCard);
  }

  @override
  Future<Result<void>> editEmployeeIdCard(
    String companyId,
    String employeeId,
    EmployeeIdCard idCard,
  ) async {
    lastSavedIdCard = idCard;
    if (_shouldFail) {
      return Result.error(Exception('editEmployeeIdCard failed'));
    }
    _idCard = idCard;
    return const Result<void>.success(null);
  }

  @override
  Future<Result<EmployeeVoteId>> getEmployeeVoteId(
    String companyId,
    String employeeId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getEmployeeVoteId failed'));
    }
    return Result.success(_voteId);
  }

  @override
  Future<Result<void>> editEmployeeVoteId(
    String companyId,
    String employeeId,
    String voteIdNumber,
  ) async {
    lastSavedVoteIdNumber = voteIdNumber;
    if (_shouldFail) {
      return Result.error(Exception('editEmployeeVoteId failed'));
    }
    _voteId = EmployeeVoteId(number: voteIdNumber);
    return const Result<void>.success(null);
  }
}
