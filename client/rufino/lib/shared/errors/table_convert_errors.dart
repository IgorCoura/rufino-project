import 'aplication_errors.dart';

class ConvertErrors {
  static Map<String, AplicationException> errors = {
    "1006": AplicationErrors.auth.authorizationErro,
  };

  static AplicationException fromErroServer(String code, String message) {
    var error = errors[code] ?? AplicationErrors.error(code, message);
    return error;
  }

  static AplicationException fromResponseBodyServer(Map<String, dynamic> body) {
    List<Map<String, String>> errorsMap = [];
    body['errors'].forEach((key, value) {
      for (var error in value) {
        if (error['code'] != null && error['message'] != null) {
          errorsMap.add({"code": error['code'], "message": error['message']});
        }
      }
    });

    if (errorsMap.isEmpty) {
      return AplicationErrors.serverError;
    }

    if (errorsMap.length == 1) {
      var exception = errors[errorsMap[0]['code']] ??
          AplicationErrors.error(
              errorsMap[0]['code']!, errorsMap[0]['message']!);
      return exception;
    }

    List<Map<String, String>> errorList = [];
    for (var errorMap in errorsMap) {
      var exception = errors[errorsMap[0]['code']];
      if (exception != null) {
        errorList.add({"code": exception.code, "message": exception.message});
        continue;
      }
      errorList
          .add({"code": errorMap['code']!, "message": errorMap['message']!});
    }

    return AplicationErrors.errorList(errorList);
  }
}
