import 'dart:convert';

import 'package:rufino/shared/errors/types/auth_errors.dart';

class AplicationErrors {
  static final _factory = AplicationErrorFactory("APP");

  static AplicationException get internalError => _factory.newError(
      1, "Houve um erro no aplicativo. Tente novamente mais tarde.");

  static AplicationException get serverError => _factory.newError(
      2, "Erro interno no servidor. Tente novamente mais tarde.");

  static AplicationException errorList(List<Map<String, String>> errors) =>
      _factory.newError(
          3, errors.map((map) => jsonEncode(map)).toList().join(","));

  static AplicationException error(String code, String message) =>
      _factory.newError(4, "Server Erro $code : $message");

  static AuthErrors get auth => AuthErrors();
}

class AplicationException implements Exception {
  const AplicationException(this.code, this.message);

  final String code;
  final String message;

  @override
  String toString() => 'AplicationError(code: $code, message: $message)';
}

class AplicationErrorFactory {
  static const maxLengthCodeAcronym = 5;
  late String _codeAcronym;

  AplicationErrorFactory(String codeAcronym) {
    this.codeAcronym = codeAcronym;
  }

  String get codeAcronym => _codeAcronym;

  set codeAcronym(String value) {
    if (value.isEmpty || value.length > maxLengthCodeAcronym) {
      throw ArgumentError(
          "Code Acronym cannot be empty or have more than $maxLengthCodeAcronym characters.");
    }
    _codeAcronym = value.toUpperCase();
  }

  newError(int code, String message) =>
      AplicationException(_createCodeError(code), message);

  String _createCodeError(int code) {
    if (code < 0) {
      throw ArgumentError("Code can't be less that 0.");
    }
    return "$codeAcronym.$code";
  }
}
