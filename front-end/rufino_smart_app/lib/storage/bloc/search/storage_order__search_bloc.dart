import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino_smart_app/storage/model/storage_order_item_model.dart';

part 'storage_order__search_event.dart';
part 'storage_order_search_state.dart';

class StorageOrderSearchBloc
    extends Bloc<StorageOrderSearchEvent, StorageOrderSearchState> {
  StorageOrderSearchBloc() : super(StorageOrderSearchInitial()) {
    on<ReturnInitialEvent>((event, emit) => emit(StorageOrderSearchInitial()));
    on<CreateItemEvent>(createItem);
  }

  Future<void> createItem(
      CreateItemEvent event, Emitter<StorageOrderSearchState> emit) async {
    emit(CreateItemState(event.item));
  }
}
