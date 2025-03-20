import 'package:rufino/shared/errors/aplication_errors.dart';

class AuthErrors {
  AuthErrors();
  static const codePrefix = "AUTH";
  final _factory = AplicationErrorFactory(codePrefix);

  AplicationException get authorizationErro => _factory.newError(1,
      "Houve um erro ao realizar a authorização. Tente novamente mais tarde.",
      callBackPage: "/");

  AplicationException get unauthenticatedAccess => _factory.newError(
      2, "Você não está autenticado. Por favor, realizar o login.",
      callBackPage: "/login");

  AplicationException get unauthorizedAccess => _factory.newError(
      3, "Você não tem autorizatição para efetuar essa função");
}
