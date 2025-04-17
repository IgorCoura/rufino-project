import 'dart:convert';
import 'dart:io';
import 'dart:math';

import 'package:http/http.dart';
import 'package:oauth2/oauth2.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';
import 'package:rufino/shared/errors/table_convert_errors.dart';
import 'package:uuid/v8.dart';

class BaseService {
  final AuthService _authService;

  BaseService(this._authService);
  Future<Map<String, String>> getHeaders(
      {String contentType = "application/json"}) async {
    var headers = <String, String>{};
    headers["Connection"] = "keep-alive";
    headers["Content-Type"] = contentType;
    headers["Accept"] = "*/*";
    headers["Authorization"] = await _authService.getAuthorizationHeader();
    return headers;
  }

  Future<Map<String, String>> getHeadersWithRequestId(
      {String contentType = "application/json", String requestId = ""}) async {
    var headers = <String, String>{};
    headers["Connection"] = "keep-alive";
    headers["x-requestid"] =
        requestId.isEmpty ? const UuidV8().generate() : requestId;
    headers["Content-Type"] = "application/json";
    headers["Accept"] = "*/*";
    headers["Authorization"] = await _authService.getAuthorizationHeader();
    return headers;
  }

  AplicationException treatUnsuccessfulResponses<T>(Response response) {
    if (response.statusCode == 401) {
      return AplicationErrors.auth.unauthenticatedAccess;
    }
    if (response.statusCode == 403) {
      return AplicationErrors.auth.unauthorizedAccess;
    }
    try {
      Map<String, dynamic> jsonResponse = jsonDecode(response.body);
      return ConvertErrors.fromResponseBodyServer(jsonResponse);
    } catch (e) {
      return AplicationErrors.serverError;
    }
  }

  Future<AplicationException> treatUnsuccessfulStreamedResponses<T>(
      StreamedResponse response) async {
    if (response.statusCode == 401) {
      throw AplicationErrors.auth.unauthenticatedAccess;
    }
    if (response.statusCode == 403) {
      throw AplicationErrors.auth.unauthorizedAccess;
    }
    var responseBody = await response.stream.bytesToString();

    try {
      Map<String, dynamic> jsonResponse = jsonDecode(responseBody);
      return ConvertErrors.fromResponseBodyServer(jsonResponse);
    } catch (e) {
      return AplicationErrors.serverError;
    }
  }

  AplicationException treatErrors(Object ex, StackTrace stacktrace) {
    switch (ex.runtimeType) {
      case const (AplicationException):
        return ex as AplicationException;
      case const (HandshakeException):
        return AplicationErrors.connectionErro;
      case const (AuthorizationException):
        var exception = ex as AuthorizationException;
        if (exception.error == "invalid_grant") {
          return AplicationErrors.auth.unauthenticatedAccess;
        }
        return AplicationErrors.connectionErro;
      default:
        return AplicationErrors.serverError;
    }
  }
}
