import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/cupertino.dart';
import 'package:rufino_app/src/login/pages/components/text_field_container.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';

part 'stock_home_event.dart';
part 'stock_home_state.dart';

class StockHomeBloc extends Bloc<StockHomeEvent, StockHomeState> {
  StockHomeBloc() : super(StockHomeInitial()) {
    on<SearchEvent>(_search);
    on<OpenDialogEvent>(_openDialog);
    on<AddQuantiyItemEvent>(_addQuantityItem);
    on<SubtractQuantiyItemEvent>(_subtractQuantityItem);
    on<ChangeQuantityEvent>(_changeQuantity);
  }

  Future<void> _search(SearchEvent event, Emitter<StockHomeState> emit) async {
    emit(StockHomeState(search: event.search));
  }

  Future<void> _openDialog(
      OpenDialogEvent event, Emitter<StockHomeState> emit) async {
    emit(DialogStockHomeState());
  }

  Future<void> _addQuantityItem(
      AddQuantiyItemEvent event, Emitter<StockHomeState> emit) async {
    if (state.quantityController == null) {
      emit(DialogStockHomeState());
    }
    if (state.quantityController!.text == "") {
      state.quantityController!.text = "1";
    } else {
      var value = int.parse(state.quantityController!.text);
      state.quantityController!.text = (value + 1).toString();
    }
  }

  Future<void> _subtractQuantityItem(
      SubtractQuantiyItemEvent event, Emitter<StockHomeState> emit) async {
    if (state.quantityController == null) {
      emit(DialogStockHomeState());
    }
    if (state.quantityController!.text == "") {
      state.quantityController!.text = "0";
    } else {
      var value = int.parse(state.quantityController!.text);
      value = value - 1 < 0 ? 0 : value - 1;
      state.quantityController!.text = value.toString();
    }
  }

  Future<void> _changeQuantity(
      ChangeQuantityEvent event, Emitter<StockHomeState> emit) async {
    if (state.quantityController == null) {
      emit(DialogStockHomeState());
    }
    state.quantityController!.text = event.value;
  }
}
