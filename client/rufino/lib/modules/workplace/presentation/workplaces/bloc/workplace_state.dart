part of 'workplace_bloc.dart';

class WorkplaceState extends Equatable {
  final bool isLoading;
  final AplicationException? exception;
  final List<Workplace> workplace;

  const WorkplaceState({
    this.isLoading = false,
    this.exception,
    this.workplace = const [],
  });

  WorkplaceState copyWith({
    bool? isLoading,
    AplicationException? exception,
    List<Workplace>? workplace,
  }) {
    return WorkplaceState(
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
      workplace: workplace ?? this.workplace,
    );
  }

  @override
  List<Object?> get props => [workplace, isLoading, exception];
}
