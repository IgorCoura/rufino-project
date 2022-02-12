part of 'stock_home_bloc.dart';

abstract class StockHomeEvent extends Equatable {
  const StockHomeEvent();

  @override
  List<Object> get props => [];
}

class SearchEvent extends StockHomeEvent {
  final String search;
  const SearchEvent(this.search);
}

class AddItemEvent extends StockHomeEvent {
  final StockOrderItemModel item;
  const AddItemEvent(this.item);
}

class ReturnInitialEvent extends StockHomeEvent {}

class AddQuantiyItemEvent extends StockHomeEvent {
  final int index;
  const AddQuantiyItemEvent(this.index);
}

class SubtractQuantiyItemEvent extends StockHomeEvent {
  final int index;
  const SubtractQuantiyItemEvent(this.index);
}

class ChangeItemEvent extends StockHomeEvent {
  final int index;
  final StockOrderItemModel item;
  const ChangeItemEvent(this.index, this.item);
}

class RemoveItemEvent extends StockHomeEvent {
  final int index;
  const RemoveItemEvent(this.index);
}

class RemoveAllItemsEvent extends StockHomeEvent {}

class FinishedStockOrderEvent extends StockHomeEvent {
  final String idTaker;
  final String idResposible;
  final BuildContext context;
  const FinishedStockOrderEvent(this.idTaker, this.idResposible, this.context);
}
