import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino_app/src/stock/model/stock_order_item_model.dart';
import 'package:rufino_app/src/stock/services/product_transaction_service.dart';

part 'stock_home_event.dart';
part 'stock_home_state.dart';

class StockHomeBloc extends Bloc<StockHomeEvent, StockHomeState> {
  final ProductTransactionService service;

  StockHomeBloc(this.service)
      : super(const StockHomeInitial(search: "", items: [])) {
    on<SearchEvent>(_search);
    on<AddItemEvent>(_addItem);
    on<ReturnInitialEvent>(_returnInitial);
    on<ChangeItemEvent>(_changeItem);
    on<AddQuantiyItemEvent>(_addQuantityItem);
    on<SubtractQuantiyItemEvent>(_subtractQuantityItem);
    on<RemoveItemEvent>(_removeItem);
    on<RemoveAllItemsEvent>(_removeAllItems);
    on<FinishedStockOrderEvent>(_finishedStockOrderEvent);
  }

  Future<void> _search(SearchEvent event, Emitter<StockHomeState> emit) async {
    emit(state.copyWith(search: event.search, items: state.items));
  }

  Future<void> _addItem(
      AddItemEvent event, Emitter<StockHomeState> emit) async {
    var items = List.of(state.items)..add(event.item);
    emit(state.copyWith(search: state.search, items: items));
  }

  Future<void> _returnInitial(
      ReturnInitialEvent event, Emitter<StockHomeState> emit) async {
    emit(StockHomeInitial(search: state.search, items: state.items));
  }

  Future<void> _changeItem(
      ChangeItemEvent event, Emitter<StockHomeState> emit) async {
    var list = List.of(state.items);
    list[event.index] = event.item;
    emit(StockHomeState(search: state.search, items: list));
  }

  Future<void> _addQuantityItem(
      AddQuantiyItemEvent event, Emitter<StockHomeState> emit) async {
    var list = List.of(state.items);
    list[event.index].quantityVariation++;
    emit(state.copyWith(items: list));
  }

  Future<void> _subtractQuantityItem(
      SubtractQuantiyItemEvent event, Emitter<StockHomeState> emit) async {
    var list = List.of(state.items);
    var value = list[event.index].quantityVariation - 1;
    list[event.index].quantityVariation = value < 0 ? 0 : value;
    emit(state.copyWith(items: list));
  }

  Future<void> _removeItem(
      RemoveItemEvent event, Emitter<StockHomeState> emit) async {
    var list = List.of(state.items);
    list.removeAt(event.index);
    emit(state.copyWith(items: list));
  }

  Future<void> _removeAllItems(
      RemoveAllItemsEvent event, Emitter<StockHomeState> emit) async {
    emit(state.copyWith(items: []));
  }

  Future<void> _finishedStockOrderEvent(
      FinishedStockOrderEvent event, Emitter<StockHomeState> emit) async {
    emit(LoadOrderState(search: state.search, items: state.items));
    await service.insertTransaction(
        state.items, event.idResposible, event.idTaker, event.withdraw);
    emit(const FinishedOrderState(search: "", items: []));
  }
}
