import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino_smart_app/storage/model/storage_order_item_model.dart';

part 'storage_order_event.dart';
part 'storage_order_state.dart';

class StorageOrderBloc extends Bloc<StorageOrderEvent, StorageOrderState> {
  StorageOrderBloc() : super(const StorageOrderInitial(itens: [])) {
    on<ReturnInitialEvent>(
        ((event, emit) => emit(StorageOrderInitial(itens: state.itens))));
    on<InsertItemInListEvent>(insertItem);
    on<EditItemEvent>(editItem);
    on<ChangeQuantityItemEvent>(changeQuantity);
    on<RemoveItemEvent>(removeItem);
    on<CreateItemEvent>(createItem);
  }

  Future<void> insertItem(
      InsertItemInListEvent event, Emitter<StorageOrderState> emit) async {
    var copy = state.copyWith(itens: List.of(state.itens));
    copy.itens.add(event.item);
    emit(copy);
  }

  Future<void> editItem(
      EditItemEvent event, Emitter<StorageOrderState> emit) async {
    emit(EditItemState(index: event.index, itens: state.itens));
  }

  Future<void> changeQuantity(
      ChangeQuantityItemEvent event, Emitter<StorageOrderState> emit) async {
    emit(state.copyWith(itens: List.of(state.itens)));
  }

  Future<void> removeItem(
      RemoveItemEvent event, Emitter<StorageOrderState> emit) async {
    var copy = state.copyWith(itens: List.of(state.itens));
    copy.itens.removeAt(event.index);
    emit(copy);
  }

  Future<void> createItem(
      CreateItemEvent event, Emitter<StorageOrderState> emit) async {
    emit(CreateItemState(item: event.item, itens: state.itens));
  }
}
