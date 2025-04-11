part of 'archive_category_bloc.dart';

sealed class ArchiveCategoryEvent extends Equatable {
  const ArchiveCategoryEvent();

  @override
  List<Object> get props => [];
}

final class InitialEvent extends ArchiveCategoryEvent {
  const InitialEvent();
}
