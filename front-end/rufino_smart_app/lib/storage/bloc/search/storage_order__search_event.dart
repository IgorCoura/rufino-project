part of 'storage_order__search_bloc.dart';

abstract class StorageOrderSearchEvent extends Equatable {
  const StorageOrderSearchEvent();

  @override
  List<Object> get props => [];
}

class ReturnInitialEvent extends StorageOrderSearchEvent {}

class CreateItemEvent extends StorageOrderSearchEvent {
  final StorageOrderItemModel item;

  const CreateItemEvent(this.item);

  @override
  List<Object> get props => [item];
}
