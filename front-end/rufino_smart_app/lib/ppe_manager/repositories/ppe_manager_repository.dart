import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:rufino_smart_app/ppe_manager/model/worker_model.dart';

class PpeManagerRepository {
  String path = "https://localhost:49153/api/";

  Future<List<WorkerModel>> getWorkers(int offset, int limit) async {
    Dio dio = Dio();
    var response =
        await dio.get(path + "v1/Worker?offset=${offset}&limit=${limit}");
    if (response.statusCode == 200) {
      var worker = List<WorkerModel>.from(
          response.data.map((e) => WorkerModel.fromJson(e)));
      return worker;
    } else {
      return [];
    }
  }
}
