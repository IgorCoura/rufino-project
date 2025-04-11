part of 'archive_category_bloc.dart';

class ArchiveCategoryState extends Equatable {
  final List<ArchiveCategory> archiveCategories;
  final List<Event> events;
  final bool isLoading;
  final AplicationException? exception;

  const ArchiveCategoryState({
    this.archiveCategories = const [],
    this.events = const [],
    this.isLoading = false,
    this.exception,
  });

  ArchiveCategoryState copyWith({
    List<ArchiveCategory>? archiveCategories,
    List<Event>? events,
    bool? isLoading,
    AplicationException? exception,
  }) {
    return ArchiveCategoryState(
      archiveCategories: archiveCategories ?? this.archiveCategories,
      events: events ?? this.events,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
    );
  }

  @override
  List<Object?> get props => [archiveCategories, events, isLoading, exception];
}
