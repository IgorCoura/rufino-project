part of 'stock_home_bloc.dart';

class StockHomeState extends Equatable {
  final String search;
  final String? idTaker;
  final String? idResponsible;
  final List<StockOrderItemModel> items;

  const StockHomeState(
      {required this.search,
      required this.items,
      this.idTaker,
      this.idResponsible});

  @override
  List<Object> get props => [search, items.hashCode];

  StockHomeState copyWith(
      {String? search,
      List<StockOrderItemModel>? items,
      String? idTaker,
      String? idResponsible}) {
    return StockHomeState(
      search: search ?? this.search,
      items: items ?? this.items,
      idTaker: idTaker ?? this.idTaker,
      idResponsible: idResponsible ?? this.idResponsible,
    );
  }
}

class StockHomeInitial extends StockHomeState {
  const StockHomeInitial({
    required String search,
    required List<StockOrderItemModel> items,
  }) : super(search: search, items: items);
}

class LoadOrderState extends StockHomeState {
  const LoadOrderState(
      {required String search, required List<StockOrderItemModel> items})
      : super(search: search, items: items);
}

class FinishedOrderState extends StockHomeState {
  const FinishedOrderState(
      {required String search, required List<StockOrderItemModel> items})
      : super(search: search, items: items);
}
