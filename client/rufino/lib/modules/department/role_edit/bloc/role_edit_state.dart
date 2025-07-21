part of 'role_edit_bloc.dart';

class RoleEditState extends Equatable {
  final bool isSavingData;
  final bool isLoading;
  final AplicationException? exception;
  final String? snackMessage;
  final Role role;
  final String positionId;
  final List<PaymentUnit> paymentUnits;
  final List<SalaryType> salaryTypes;

  const RoleEditState({
    this.isSavingData = false,
    this.isLoading = false,
    this.exception,
    this.snackMessage,
    this.role = const Role.empty(),
    this.positionId = "",
    this.paymentUnits = const [],
    this.salaryTypes = const [],
  });

  RoleEditState copyWith({
    bool? isSavingData,
    bool? isLoading,
    AplicationException? exception,
    String? snackMessage,
    Role? role,
    String? positionId,
    List<PaymentUnit>? paymentUnits,
    List<SalaryType>? salaryTypes,
  }) {
    return RoleEditState(
      isSavingData: isSavingData ?? this.isSavingData,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
      role: role ?? this.role,
      positionId: positionId ?? this.positionId,
      paymentUnits: paymentUnits ?? this.paymentUnits,
      salaryTypes: salaryTypes ?? this.salaryTypes,
    );
  }

  @override
  List<Object?> get props => [
        isSavingData,
        isLoading,
        exception,
        snackMessage,
        role,
        positionId,
        paymentUnits,
        salaryTypes,
      ];
}
