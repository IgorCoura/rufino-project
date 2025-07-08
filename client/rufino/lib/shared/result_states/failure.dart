import 'package:rufino/shared/errors/aplication_errors.dart';
import 'package:rufino/shared/errors/table_convert_errors.dart';

class Failure implements Exception {
  List<AplicationException> errors = [];

  Failure(AplicationException error) {
    errors.add(error);
  }

  Failure.fromListErrors(this.errors);

  factory Failure.fromJson(List<dynamic> json) {
    var errors = json
        .map((errorJson) => ConvertErrors.fromErroServer(
            errorJson["code"], errorJson["message"]))
        .toList();
    return Failure.fromListErrors(errors);
  }
}
