part of 'position_edit_bloc.dart';

class PositionEditState extends Equatable {
  final bool isSavingData;
  final bool isLoading;
  final AplicationException? exception;
  final String? snackMessage;
  final Position position;
  final String departmentId;

  const PositionEditState({
    this.isSavingData = false,
    this.isLoading = false,
    this.exception,
    this.snackMessage,
    this.position = const Position.empty(),
    this.departmentId = "",
  });

  PositionEditState copyWith({
    bool? isSavingData,
    bool? isLoading,
    AplicationException? exception,
    String? snackMessage,
    Position? position,
    String? departmentId,
  }) {
    return PositionEditState(
      isSavingData: isSavingData ?? this.isSavingData,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
      position: position ?? this.position,
      departmentId: departmentId ?? this.departmentId,
    );
  }

  @override
  List<Object?> get props => [
        isSavingData,
        isLoading,
        exception,
        snackMessage,
        position,
        departmentId,
      ];
}
