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

class AddQuantiyItemEvent extends StockHomeEvent {}

class SubtractQuantiyItemEvent extends StockHomeEvent {}

class OpenDialogEvent extends StockHomeEvent {}

class ChangeQuantityEvent extends StockHomeEvent {
  final String value;
  const ChangeQuantityEvent(this.value);
}
