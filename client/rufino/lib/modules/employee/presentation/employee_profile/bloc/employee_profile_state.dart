part of 'employee_profile_bloc.dart';

class EmployeeProfileState extends Equatable {
  final Company company;
  final bool isEditingName;
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

  const EmployeeProfileState({
    this.company = const Company.empty(),
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
  });

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
      ];
}
