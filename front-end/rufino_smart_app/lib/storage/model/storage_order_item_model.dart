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

  void changeQuantity(int value) {
    quantity = value;
    if (quantity < 0) {
      quantity = 0;
    }
  }

  @override
  List<Object?> get props =>
      [id, name, description, quantity, quantityInStorage, unity];
}
