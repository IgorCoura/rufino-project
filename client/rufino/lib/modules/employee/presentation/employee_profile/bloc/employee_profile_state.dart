part of 'employee_profile_bloc.dart';

class EmployeeProfileState extends Equatable {
  final Company company;
  final bool isEditingName;
  final bool isDocumentSigningOptionsName;
  final Employee employee;
  final bool isLoading;
  final Contact contact;
  final AplicationException? exception;
  final bool isSavingData;
  final String? snackMessage;
  final Address address;
  final PersonalInfo personalInfo;
  final PersonalInfoSeletionOptions personalInfoSeletionOptions;
  final IdCard idCard;
  final VoteId voteId;
  final MilitaryDocument militaryDocument;
  final MedicalAdmissionExam medicalAdmissionExam;
  final RoleInfo roleInfo;
  final List<Department> listDepartment;
  final List<Position> listPosition;
  final List<Role> listRolen;
  final Workplace workplace;
  final List<Workplace> listWorkplace;
  final List<EmployeeContract> listContracts;
  final List<EmployeeContractType> listContractTypes;
  final List<DocumentSigningOptions> listDocumentSigningOptions;

  const EmployeeProfileState(
      {this.company = const Company.empty(),
      this.isEditingName = false,
      this.employee = const Employee.empty(),
      this.isLoading = false,
      this.contact = const Contact.loading(),
      this.exception,
      this.isSavingData = false,
      this.snackMessage,
      this.address = const Address.loading(),
      this.personalInfo = const PersonalInfo.loading(),
      this.personalInfoSeletionOptions = const PersonalInfoSeletionOptions(),
      this.idCard = const IdCard.loading(),
      this.voteId = const VoteId.loading(),
      this.militaryDocument = const MilitaryDocument.loading(),
      this.medicalAdmissionExam = const MedicalAdmissionExam.loading(),
      this.roleInfo = const RoleInfo.loading(),
      this.listDepartment = const [],
      this.listPosition = const [],
      this.listRolen = const [],
      this.workplace = const Workplace.empty(),
      this.listWorkplace = const [],
      this.listContracts = const [],
      this.listContractTypes = const [],
      this.listDocumentSigningOptions = const [],
      this.isDocumentSigningOptionsName = false});

  EmployeeProfileState copyWith({
    Company? company,
    bool? isEditingName,
    Employee? employee,
    List<Status>? listStatus,
    bool? isLoading,
    Contact? contact,
    AplicationException? exception,
    bool? isSavingData,
    String? snackMessage,
    Address? address,
    PersonalInfo? personalInfo,
    PersonalInfoSeletionOptions? personalInfoSeletionOptions,
    IdCard? idCard,
    VoteId? voteId,
    MilitaryDocument? militaryDocument,
    MedicalAdmissionExam? medicalAdmissionExam,
    RoleInfo? roleInfo,
    List<Department>? listDepartment,
    List<Position>? listPosition,
    List<Role>? listRolen,
    Workplace? workplace,
    List<Workplace>? listWorkplace,
    List<EmployeeContract>? listContracts,
    List<EmployeeContractType>? listContractTypes,
    List<DocumentSigningOptions>? listDocumentSigningOptions,
    bool? isDocumentSigningOptionsName,
  }) =>
      EmployeeProfileState(
        company: company ?? this.company,
        isEditingName: isEditingName ?? this.isEditingName,
        employee: employee ?? this.employee,
        isLoading: isLoading ?? this.isLoading,
        contact: contact ?? this.contact,
        exception: exception ?? this.exception,
        isSavingData: isSavingData ?? this.isSavingData,
        snackMessage: snackMessage ?? this.snackMessage,
        address: address ?? this.address,
        personalInfo: personalInfo ?? this.personalInfo,
        personalInfoSeletionOptions:
            personalInfoSeletionOptions ?? this.personalInfoSeletionOptions,
        idCard: idCard ?? this.idCard,
        voteId: voteId ?? this.voteId,
        militaryDocument: militaryDocument ?? this.militaryDocument,
        medicalAdmissionExam: medicalAdmissionExam ?? this.medicalAdmissionExam,
        roleInfo: roleInfo ?? this.roleInfo,
        listDepartment: listDepartment ?? this.listDepartment,
        listPosition: listPosition ?? this.listPosition,
        listRolen: listRolen ?? this.listRolen,
        workplace: workplace ?? this.workplace,
        listWorkplace: listWorkplace ?? this.listWorkplace,
        listContracts: listContracts ?? this.listContracts,
        listContractTypes: listContractTypes ?? this.listContractTypes,
        listDocumentSigningOptions:
            listDocumentSigningOptions ?? this.listDocumentSigningOptions,
        isDocumentSigningOptionsName:
            isDocumentSigningOptionsName ?? this.isDocumentSigningOptionsName,
      );

  @override
  List<Object?> get props => [
        company,
        isEditingName,
        employee,
        isLoading,
        contact,
        exception,
        isSavingData,
        snackMessage,
        address,
        personalInfo,
        personalInfoSeletionOptions,
        idCard,
        voteId,
        militaryDocument,
        medicalAdmissionExam,
        roleInfo,
        listDepartment.hashCode,
        listPosition.hashCode,
        listRolen.hashCode,
        workplace,
        listWorkplace.hashCode,
        listContracts.hashCode,
        listContractTypes.hashCode,
        listDocumentSigningOptions.hashCode,
        isDocumentSigningOptionsName,
      ];
}
