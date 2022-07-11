import 'package:dio/dio.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/utils/stock_constants.dart';

class ProductRepository {
  final Dio _dio;

  ProductRepository(this._dio);

  Future<List<Product>> getAllProducts(DateTime modificationDate) async {
    try {
      final response = await _dio.get<List>(
          "$kHttpStockApi/api/v1/Product?modificationDate=${modificationDate.toString()}");
      if (response.statusCode == 200) {
        return response.data!.map((e) => Product.fromData(e)).toList();
      } else {
        throw Exception(response.statusMessage);
      }
    } on DioError {
      rethrow;
    }
  }
}
