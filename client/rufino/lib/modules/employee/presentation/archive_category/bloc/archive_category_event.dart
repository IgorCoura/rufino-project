part of 'archive_category_bloc.dart';

sealed class ArchiveCategoryEvent extends Equatable {
  const ArchiveCategoryEvent();

  @override
  List<Object> get props => [];
}

final class InitialEvent extends ArchiveCategoryEvent {
  const InitialEvent();
}

final class SaveEvent extends ArchiveCategoryEvent {
  final List<Object> changes;
  final ArchiveCategory category;
  const SaveEvent(this.category, this.changes);
  @override
  List<Object> get props => [category, changes];
}

final class SnackMessageWasShow extends ArchiveCategoryEvent {
  const SnackMessageWasShow();
}

final class CreateNewArchiveCategoryEvent extends ArchiveCategoryEvent {
  final ArchiveCategory category;
  const CreateNewArchiveCategoryEvent(this.category);
  @override
  List<Object> get props => [category];
}
