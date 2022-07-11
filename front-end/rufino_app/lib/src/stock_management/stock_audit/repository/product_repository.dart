import 'package:dio/dio.dart';
import 'package:rufino_app/src/stock/utils/stock_constants.dart';

import '../models/product_model.dart';

class ProductRepository {
  final Dio _dio;

  ProductRepository(this._dio);

  Future<List<ProductModel>> getAllProducts() async {
    try {
      final response = await _dio.get<List>("$kHttpStockApi/api/v1/Product");
      if (response.statusCode == 200) {
        return response.data!.map((e) => ProductModel.fromData(e)).toList();
      } else {
        throw Exception(response.statusMessage);
      }
    } on DioError {
      rethrow;
    }
  }
}
