import 'dart:convert';
import 'dart:io';

import 'package:dio/dio.dart';
import 'package:drift/drift.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/model/stock_order_item_model.dart';
import 'package:rufino_app/src/stock/utils/stock_constants.dart';

class ProductTransactionRepository {
  final Dio _dio;

  ProductTransactionRepository(this._dio);

  Future<List<ProductTransaction>> postTransactions(
      Iterable<ProductTransaction> items) async {
    try {
      var data = items.map((e) {
        return {
          "deviceId": e.id,
          "productId": e.idProduct,
          "quantityVariation": e.quantityVariation,
          "date": e.date.toUtc().toIso8601String(),
          "responsibleId": e.idResponsible,
          "takerId": e.idTaker,
        };
      }).toList();

      var dataEncode = jsonEncode(data);

      final response =
          await _dio.post<List>("$kHttpStockApi/api/v1/ProductTransaction",
              data: dataEncode,
              options: Options(headers: {
                HttpHeaders.contentTypeHeader: "application/json",
              }));
      return response.data!
          .map((e) => ProductTransaction(
                id: e["deviceId"],
                idTransactionServer: e["id"],
                idProduct: e["productId"],
                idResponsible: e["responsibleId"],
                idTaker: e["takerId"],
                date: DateTime.parse(e["date"]),
                quantityVariation: e["quantityVariation"],
              ))
          .toList();
    } catch (e) {
      rethrow;
    }
  }
}
