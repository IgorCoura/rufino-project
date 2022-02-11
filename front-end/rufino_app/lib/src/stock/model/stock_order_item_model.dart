import 'package:equatable/equatable.dart';

class StockOrderItemModel extends Equatable {
  final String idProduct;
  final String name;
  final String description;
  int quantityVariation;
  final String unity;

  StockOrderItemModel(this.idProduct, this.name, this.description,
      this.quantityVariation, this.unity);

  @override
  List<Object?> get props =>
      [idProduct, name, description, quantityVariation, unity];
}
