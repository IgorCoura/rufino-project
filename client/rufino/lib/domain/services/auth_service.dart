import 'dart:async';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:oauth2/oauth2.dart' as oauth2;
import 'package:rufino/shared/errors/aplication_errors.dart';

class AuthService {
  final Uri _authorizationEndpoint =
      Uri.parse(const String.fromEnvironment("authorization_endpoint"));
  final Uri _endSessionEndpoint =
      Uri.parse(const String.fromEnvironment("end_session_endpoint"));
  final String _identifier = const String.fromEnvironment("identifier");
  final String _secret = const String.fromEnvironment("secret");
  final _storage = const FlutterSecureStorage();
  final _keyStorage = "credentials";

  AuthService();

  Future<void> logIn({
    required String username,
    required String password,
  }) async {
    try {
      var client = await oauth2.resourceOwnerPasswordGrant(
          _authorizationEndpoint, username, password,
          identifier: _identifier, secret: _secret);
      await _storage.write(
          key: _keyStorage, value: client.credentials.toJson());
      client.close();
      return;
    } catch (ex) {
      throw AplicationErrors.auth.unauthenticatedAccess;
    }
  }

  Future<String> getToken() async {
    var credentials = await getCredentials();
    return credentials.accessToken;
  }

  Future<oauth2.Credentials> getCredentials() async {
    var credentials = await _recoverFromStorageCredentials();
    if (credentials.isExpired) {
      if (credentials.canRefresh == false) {
        throw AplicationErrors.auth.unauthenticatedAccess;
      }
      await credentials.refresh(identifier: _identifier, secret: _secret);
    }
    return credentials;
  }

  Future<oauth2.Credentials> _recoverFromStorageCredentials() async {
    var credentialsJson = await _storage.read(key: _keyStorage);
    if (credentialsJson == null) {
      throw AplicationErrors.auth.unauthenticatedAccess;
    }
    var creadentials = oauth2.Credentials.fromJson(credentialsJson);
    return creadentials;
  }

  Future<void> logOut() async {
    var credentials = await getCredentials();
    var client = oauth2.Client(credentials);
    Map<String, String> authData = {
      "client_id": _identifier,
      "client_secret": _secret,
      "refresh_token": credentials.refreshToken!
    };
    await client.post(_endSessionEndpoint, body: authData);
    _storage.delete(key: _keyStorage);
    client.close();
  }
}
