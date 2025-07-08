part of 'role_edit_bloc.dart';

class RoleEditState extends Equatable {
  final bool isSavingData;
  final bool isLoading;
  final AplicationException? exception;
  final String? snackMessage;
  final Role role;
  final String positionId;
  final List<PaymentUnit> paymentUnits = const [
    PaymentUnit("1", ""),
    PaymentUnit("2", ""),
    PaymentUnit("3", ""),
  ];
  final List<SalaryType> salaryTypes = const [
    SalaryType("1", "BRL"),
    SalaryType("2", "USD"),
    SalaryType("3", "EUR"),
  ];

  const RoleEditState({
    this.isSavingData = false,
    this.isLoading = false,
    this.exception,
    this.snackMessage,
    this.role = const Role.empty(),
    this.positionId = "",
  });

  RoleEditState copyWith({
    bool? isSavingData,
    bool? isLoading,
    AplicationException? exception,
    String? snackMessage,
    Role? role,
    String? positionId,
  }) {
    return RoleEditState(
      isSavingData: isSavingData ?? this.isSavingData,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
      role: role ?? this.role,
      positionId: positionId ?? this.positionId,
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
      ];
}
