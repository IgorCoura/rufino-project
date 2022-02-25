import 'package:dio/dio.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/utils/stock_constants.dart';

class WorkerRepository {
  final Dio _dio;

  WorkerRepository(this._dio);

  Future<List<Worker>> getAllWorkers(DateTime modificationDate) async {
    try {
      final response = await _dio.get<List>(
          "$kHttpStockApi/api/v1/Worker?modificationDate=${modificationDate.toString()}");
      if (response.statusCode == 200) {
        return response.data!.map((e) => Worker.fromData(e)).toList();
      } else {
        throw Exception(response.statusMessage);
      }
    } catch (e) {
      rethrow;
    }
  }
}
