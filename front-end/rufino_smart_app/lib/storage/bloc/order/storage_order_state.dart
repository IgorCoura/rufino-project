part of 'storage_order_bloc.dart';

class StorageOrderState extends Equatable {
  final List<StorageOrderItemModel> itens;
  const StorageOrderState({required this.itens});

  StorageOrderState copyWith({List<StorageOrderItemModel>? itens}) {
    return StorageOrderState(itens: itens ?? this.itens);
  }

  @override
  List<Object> get props => [itens.hashCode];
}

class StorageOrderInitial extends StorageOrderState {
  const StorageOrderInitial({required List<StorageOrderItemModel> itens})
      : super(itens: itens);
}

class EditItemState extends StorageOrderState {
  final int index;
  const EditItemState(
      {required this.index, required List<StorageOrderItemModel> itens})
      : super(itens: itens);
}

class CreateItemState extends StorageOrderState {
  final StorageOrderItemModel item;
  const CreateItemState(
      {required this.item, required List<StorageOrderItemModel> itens})
      : super(itens: itens);
}
