class ProductModel {
  final String id;
  final String name;
  final String description;
  final String section;
  final String category;
  final String unity;
  final int quantity;
  ProductModel(
      {required this.id,
      required this.name,
      required this.description,
      required this.section,
      required this.category,
      required this.unity,
      required this.quantity});
  factory ProductModel.fromData(Map<String, dynamic> data, {String? prefix}) {
    final effectivePrefix = prefix ?? '';
    return ProductModel(
      id: data['id'],
      name: data['name'],
      description: data['description'],
      section: data['section'],
      category: data['category'],
      unity: data['unity'],
      quantity: data['quantity'],
    );
  }
}
