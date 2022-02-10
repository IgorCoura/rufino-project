part of 'stock_home_bloc.dart';

class StockHomeState extends Equatable {
  final String search;
  final TextEditingController? quantityController;
  const StockHomeState({
    this.search = "",
    this.quantityController,
  });

  @override
  List<Object> get props => [search, quantityController.hashCode];
}

class StockHomeInitial extends StockHomeState {}

class SearchStockHomeState extends StockHomeState {
  const SearchStockHomeState();

  @override
  List<Object> get props => [];
}

class DialogStockHomeState extends StockHomeState {
  DialogStockHomeState() : super(quantityController: TextEditingController());
}
