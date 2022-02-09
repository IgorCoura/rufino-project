import 'package:equatable/equatable.dart';

class StorageOrderItemModel extends Equatable {
  String id;
  String name;
  String description;
  int quantity;
  int quantityInStorage;
  String unity;

  StorageOrderItemModel(
      this.id, this.name, this.description, this.quantityInStorage, this.unity,
      {this.quantity = 0});

  void addQuantity() {
    quantity++;
  }

  void subtractQuantity() {
    quantity--;
  }

  @override
  List<Object?> get props =>
      [id, name, description, quantity, quantityInStorage, unity];
}
