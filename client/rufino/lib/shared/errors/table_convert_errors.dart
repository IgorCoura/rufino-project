import 'aplication_errors.dart';

class ConvertErrors {
  static Map<String, AplicationException> errors = {
    "1006": AplicationErrors.auth.authorizationErro,
  };

  static AplicationException fromErroServer(String code, String message) {
    var error = errors[code] ?? AplicationErrors.serverError(code, message);
    return error;
  }
}
