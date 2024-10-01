import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';
import 'package:rufino/shared/errors/table_convert_errors.dart';
import 'package:uuid/v8.dart';

class BaseService {
  final AuthService _authService;

  BaseService(this._authService);
  Future<Map<String, String>> getHeaders() async {
    var headers = <String, String>{};
    headers["Connection"] = "keep-alive";
    headers["Content-Type"] = "application/json";
    headers["Accept"] = "*/*";
    headers["Authorization"] = await _authService.getAuthorizationHeader();
    return headers;
  }

  Future<Map<String, String>> getHeadersWithRequestId() async {
    var headers = <String, String>{};
    headers["Connection"] = "keep-alive";
    headers["x-requestid"] = const UuidV8().generate();
    headers["Content-Type"] = "application/json";
    headers["Accept"] = "*/*";
    headers["Authorization"] = await _authService.getAuthorizationHeader();
    return headers;
  }

  T treatUnsuccessfulResponses<T>(Response response) {
    if (response.statusCode == 401 || response.statusCode == 403) {
      throw AplicationErrors.auth.unauthorizedAccess;
    }
    Map<String, dynamic> jsonResponse = jsonDecode(response.body);
    throw ConvertErrors.fromResponseBodyServer(jsonResponse);
  }

  AplicationException treatErrors(Object ex, StackTrace stacktrace) {
    switch (ex.runtimeType) {
      case const (AplicationException):
        return ex as AplicationException;
      case const (HandshakeException):
        return AplicationErrors.connectionErro;
      default:
        return AplicationErrors.serverError;
    }
  }
}
