part of 'workplace_edit_bloc.dart';

class WorkplaceEditState extends Equatable {
  final bool isSavingData;
  final bool isLoading;
  final AplicationException? exception;
  final String? snackMessage;
  final Workplace workplace;

  const WorkplaceEditState({
    this.isSavingData = false,
    this.isLoading = false,
    this.exception,
    this.snackMessage,
    this.workplace = const Workplace.empty(),
  });

  WorkplaceEditState copyWith({
    bool? isSavingData,
    bool? isLoading,
    AplicationException? exception,
    String? snackMessage,
    Workplace? workplace,
  }) {
    return WorkplaceEditState(
      isSavingData: isSavingData ?? this.isSavingData,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
      workplace: workplace ?? this.workplace,
    );
  }

  @override
  List<Object?> get props => [
        isSavingData,
        isLoading,
        exception,
        snackMessage,
        workplace,
      ];
}
