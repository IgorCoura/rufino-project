part of 'storage_order__search_bloc.dart';

class StorageOrderSearchState extends Equatable {
  const StorageOrderSearchState();
  @override
  List<Object> get props => [];
}

class StorageOrderSearchInitial extends StorageOrderSearchState {}

class CreateItemState extends StorageOrderSearchState {
  final StorageOrderItemModel item;

  const CreateItemState(this.item);

  @override
  List<Object> get props => [item];
}
