part of 'stock_home_bloc.dart';

class StockHomeState extends Equatable {
  final String search;

  final List<StockOrderItemModel> items;
  const StockHomeState({
    required this.search,
    required this.items,
  });

  @override
  List<Object> get props => [search, items.hashCode];

  StockHomeState copyWith({
    String? search,
    List<StockOrderItemModel>? items,
  }) {
    return StockHomeState(
      search: search ?? this.search,
      items: items ?? this.items,
    );
  }
}

class StockHomeInitial extends StockHomeState {
  StockHomeInitial({
    required String search,
    required List<StockOrderItemModel> items,
  }) : super(search: search, items: items);
}

class SearchStockHomeState extends StockHomeState {
  const SearchStockHomeState({
    required search,
    required items,
  }) : super(search: search, items: items);

  @override
  List<Object> get props => [];
}
