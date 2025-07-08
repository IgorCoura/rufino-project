part of 'archive_category_bloc.dart';

class ArchiveCategoryState extends Equatable {
  final List<ArchiveCategory> archiveCategories;
  final List<Event> events;
  final bool isLoading;
  final bool isSavingData;
  final String? snackMessage;
  final AplicationException? exception;

  const ArchiveCategoryState({
    this.archiveCategories = const [],
    this.events = const [],
    this.isLoading = false,
    this.exception,
    this.isSavingData = false,
    this.snackMessage,
  });

  ArchiveCategoryState copyWith({
    List<ArchiveCategory>? archiveCategories,
    List<Event>? events,
    bool? isLoading,
    bool? isSavingData,
    AplicationException? exception,
    String? snackMessage,
  }) {
    return ArchiveCategoryState(
      archiveCategories: archiveCategories ?? this.archiveCategories,
      events: events ?? this.events,
      isLoading: isLoading ?? this.isLoading,
      isSavingData: isSavingData ?? this.isSavingData,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
    );
  }

  @override
  List<Object?> get props => [
        archiveCategories,
        events,
        isLoading,
        isSavingData,
        exception,
        snackMessage
      ];
}
