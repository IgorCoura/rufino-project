part of 'employee_profile_bloc.dart';

class EmployeeProfileState extends Equatable {
  final Company? company;
  final bool isEditingName;
  final Employee employee;
  final bool isLoading;
  late final Contact contact;
  final AplicationException? exception;
  final bool isSavingData;
  final String? snackMessage;
  late final Address address;

  EmployeeProfileState({
    this.company,
    this.isEditingName = false,
    required this.employee,
    this.isLoading = false,
    Contact? contact,
    this.exception,
    this.isSavingData = false,
    this.snackMessage,
    Address? address,
  }) {
    this.contact = contact ?? Contact.loadingContact;
    this.address = address ?? Address.loading;
  }

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
          address: address ?? this.address);
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
      ];
}
