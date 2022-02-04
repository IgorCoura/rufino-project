part of 'storage_order_bloc.dart';

abstract class StorageOrderEvent extends Equatable {
  const StorageOrderEvent();

  @override
  List<Object> get props => [];
}

class InsertItemInListEvent extends StorageOrderEvent {
  final StorageOrderItemModel item;
  const InsertItemInListEvent(this.item);
}

class EditItemEvent extends StorageOrderEvent {
  final int index;
  const EditItemEvent(this.index);
}

class ChangeQuantityItemEvent extends StorageOrderEvent {
  const ChangeQuantityItemEvent();
}

class RemoveItemEvent extends StorageOrderEvent {
  final int index;
  const RemoveItemEvent(this.index);
}

class ReturnInitialEvent extends StorageOrderEvent {}

class CreateItemEvent extends StorageOrderEvent {
  final StorageOrderItemModel item;
  CreateItemEvent(this.item);
}
