import 'dart:typed_data';

import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/employee_contact.dart';
import 'package:rufino_v2/domain/entities/employee_document.dart';
import 'package:rufino_v2/domain/entities/employee_contract.dart';
import 'package:rufino_v2/domain/entities/employee_dependent.dart';
import 'package:rufino_v2/domain/entities/employee_id_card.dart';
import 'package:rufino_v2/domain/entities/employee_personal_info.dart';
import 'package:rufino_v2/domain/entities/employee_profile.dart';
import 'package:rufino_v2/domain/entities/employee_medical_exam.dart';
import 'package:rufino_v2/domain/entities/employee_military_document.dart';
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

  EmployeeMilitaryDocument _militaryDocument = const EmployeeMilitaryDocument(
    number: 'RM-12345',
    type: 'Reservista',
    isRequired: true,
  );

  EmployeeMedicalExam _medicalExam = const EmployeeMedicalExam(
    dateExam: '15/01/2026',
    validityExam: '15/01/2027',
  );

  List<EmployeeDependent> _dependents = const [
    EmployeeDependent(
      originalName: 'Maria Silva',
      name: 'Maria Silva',
      genderId: '2',
      dependencyTypeId: '1',
      cpf: '111.444.777-35',
      motherName: 'Ana',
      fatherName: 'João',
      dateOfBirth: '01/01/2010',
      birthCity: 'São Paulo',
      birthState: 'SP',
      nationality: 'Brasileira',
    ),
  ];

  List<EmployeeContractInfo> _contracts = const [
    EmployeeContractInfo(
      initDate: '01/01/2026',
      finalDate: '',
      typeId: '1',
      typeName: 'CLT',
    ),
  ];

  List<SelectionOption> _contractTypes = const [
    SelectionOption(id: '1', name: 'CLT'),
    SelectionOption(id: '2', name: 'Aprendiz'),
  ];

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

  /// The military document returned by [getMilitaryDocument].
  void setMilitaryDocument(EmployeeMilitaryDocument doc) =>
      _militaryDocument = doc;

  /// The medical exam returned by [getMedicalExam].
  void setMedicalExam(EmployeeMedicalExam exam) => _medicalExam = exam;

  List<EmployeeDocument> _documentsList = const [
    EmployeeDocument(
      id: 'doc-1',
      name: 'Contrato de Trabalho',
      description: 'Contrato CLT',
      statusId: '3',
      statusName: 'OK',
      isSignable: false,
      canGenerateDocument: true,
      totalUnitsCount: 1,
      units: [
        DocumentUnit(
          id: 'unit-1',
          statusId: '2',
          statusName: 'OK',
          date: '01/01/2026',
          validity: '',
          createdAt: '01/01/2026',
          hasFile: true,
          name: 'contrato.pdf',
        ),
      ],
    ),
  ];

  /// The documents returned by [getDocuments].
  void setDocumentsList(List<EmployeeDocument> docs) =>
      _documentsList = docs;

  /// The dependents returned by [getDependents].
  void setDependents(List<EmployeeDependent> deps) => _dependents = deps;

  List<SelectionOption> _signingOptions = const [
    SelectionOption(id: '1', name: 'Assinatura Fisica'),
    SelectionOption(id: '2', name: 'Assinatura Digital e Whatsapp'),
  ];

  /// The contracts returned by [getContracts].
  void setContracts(List<EmployeeContractInfo> c) => _contracts = c;

  /// The contract types returned by [getContractTypes].
  void setContractTypes(List<SelectionOption> t) => _contractTypes = t;

  // ─── Captured call arguments (for assertion) ──────────────────────────────

  String? lastSavedWorkplaceId;
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
  String? lastSavedMilitaryDocumentNumber;
  String? lastSavedMilitaryDocumentType;
  String? lastSavedMedicalExamDate;
  String? lastSavedMedicalExamValidity;
  String? lastSavedRoleId;
  EmployeeDependent? lastCreatedDependent;
  EmployeeDependent? lastEditedDependent;
  String? lastRemovedDependentName;

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

  @override
  Future<Result<EmployeeMilitaryDocument>> getMilitaryDocument(
    String companyId,
    String employeeId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getMilitaryDocument failed'));
    }
    return Result.success(_militaryDocument);
  }

  @override
  Future<Result<void>> editMilitaryDocument(
    String companyId,
    String employeeId,
    String number,
    String type,
  ) async {
    lastSavedMilitaryDocumentNumber = number;
    lastSavedMilitaryDocumentType = type;
    if (_shouldFail) {
      return Result.error(Exception('editMilitaryDocument failed'));
    }
    _militaryDocument = EmployeeMilitaryDocument(
      number: number,
      type: type,
      isRequired: _militaryDocument.isRequired,
    );
    return const Result<void>.success(null);
  }

  @override
  Future<Result<EmployeeMedicalExam>> getMedicalExam(
    String companyId,
    String employeeId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getMedicalExam failed'));
    }
    return Result.success(_medicalExam);
  }

  @override
  Future<Result<void>> editMedicalExam(
    String companyId,
    String employeeId,
    String dateExam,
    String validityExam,
  ) async {
    lastSavedMedicalExamDate = dateExam;
    lastSavedMedicalExamValidity = validityExam;
    if (_shouldFail) {
      return Result.error(Exception('editMedicalExam failed'));
    }
    _medicalExam = EmployeeMedicalExam(
      dateExam: dateExam,
      validityExam: validityExam,
    );
    return const Result<void>.success(null);
  }

  @override
  Future<Result<void>> editEmployeeRole(
    String companyId,
    String employeeId,
    String roleId,
  ) async {
    lastSavedRoleId = roleId;
    if (_shouldFail) {
      return Result.error(Exception('editEmployeeRole failed'));
    }
    return const Result<void>.success(null);
  }

  @override
  Future<Result<List<EmployeeDependent>>> getDependents(
    String companyId,
    String employeeId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getDependents failed'));
    }
    return Result.success(_dependents);
  }

  @override
  Future<Result<void>> createDependent(
    String companyId,
    String employeeId,
    EmployeeDependent dependent,
  ) async {
    lastCreatedDependent = dependent;
    if (_shouldFail) {
      return Result.error(Exception('createDependent failed'));
    }
    _dependents = [..._dependents, dependent];
    return const Result<void>.success(null);
  }

  @override
  Future<Result<void>> editDependent(
    String companyId,
    String employeeId,
    EmployeeDependent dependent,
  ) async {
    lastEditedDependent = dependent;
    if (_shouldFail) {
      return Result.error(Exception('editDependent failed'));
    }
    return const Result<void>.success(null);
  }

  @override
  Future<Result<void>> removeDependent(
    String companyId,
    String employeeId,
    String dependentName,
  ) async {
    lastRemovedDependentName = dependentName;
    if (_shouldFail) {
      return Result.error(Exception('removeDependent failed'));
    }
    _dependents =
        _dependents.where((d) => d.originalName != dependentName).toList();
    return const Result<void>.success(null);
  }

  @override
  Future<Result<void>> editEmployeeWorkplace(
    String companyId,
    String employeeId,
    String workplaceId,
  ) async {
    lastSavedWorkplaceId = workplaceId;
    if (_shouldFail) {
      return Result.error(Exception('editEmployeeWorkplace failed'));
    }
    return const Result<void>.success(null);
  }

  @override
  Future<Result<List<EmployeeContractInfo>>> getContracts(
    String companyId,
    String employeeId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getContracts failed'));
    }
    return Result.success(_contracts);
  }

  @override
  Future<Result<List<SelectionOption>>> getContractTypes(
      String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getContractTypes failed'));
    }
    return Result.success(_contractTypes);
  }

  @override
  Future<Result<void>> createContract(
    String companyId,
    String employeeId,
    String initDate,
    String contractTypeId,
    String registration,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('createContract failed'));
    }
    return const Result<void>.success(null);
  }

  @override
  Future<Result<void>> finishContract(
    String companyId,
    String employeeId,
    String finalDate,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('finishContract failed'));
    }
    return const Result<void>.success(null);
  }

  @override
  Future<Result<List<SelectionOption>>> getDocumentSigningOptions(
      String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getDocumentSigningOptions failed'));
    }
    return Result.success(_signingOptions);
  }

  @override
  Future<Result<void>> editDocumentSigningOptions(
    String companyId,
    String employeeId,
    String optionId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('editDocumentSigningOptions failed'));
    }
    return const Result<void>.success(null);
  }

  @override
  Future<Result<List<EmployeeDocument>>> getDocuments(
    String companyId,
    String employeeId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('getDocuments failed'));
    }
    return Result.success(_documentsList);
  }

  @override
  Future<Result<EmployeeDocument>> getDocumentById(
    String companyId,
    String employeeId,
    String documentId, {
    int pageNumber = 1,
    int pageSize = 10,
    int? statusId,
  }) async {
    if (_shouldFail) {
      return Result.error(Exception('getDocumentById failed'));
    }
    final doc =
        _documentsList.where((d) => d.id == documentId).firstOrNull;
    if (doc == null) {
      return Result.error(Exception('Document not found'));
    }
    return Result.success(doc);
  }

  @override
  Future<Result<void>> createDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('createDocumentUnit failed'));
    }
    return const Result<void>.success(null);
  }

  @override
  Future<Result<void>> editDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
    String date,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('editDocumentUnit failed'));
    }
    return const Result<void>.success(null);
  }

  @override
  Future<Result<void>> setDocumentUnitNotApplicable(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
  ) async {
    if (_shouldFail) {
      return Result.error(Exception('setDocumentUnitNotApplicable failed'));
    }
    return const Result<void>.success(null);
  }
}
