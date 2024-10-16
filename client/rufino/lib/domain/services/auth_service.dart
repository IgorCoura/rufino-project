import 'dart:async';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:oauth2/oauth2.dart' as oauth2;
import 'package:rufino/shared/errors/aplication_errors.dart';
import 'package:jwt_decoder/jwt_decoder.dart';

class AuthService {
  final Uri _authorizationEndpoint =
      Uri.parse(const String.fromEnvironment("authorization_endpoint"));
  final Uri _endSessionEndpoint =
      Uri.parse(const String.fromEnvironment("end_session_endpoint"));
  final String _identifier = const String.fromEnvironment("identifier");
  final String _secret = const String.fromEnvironment("secret");
  final FlutterSecureStorage _storage;
  final _keyStorage = "credentials";
  oauth2.Credentials? _credentials;

  AuthService(this._storage);

  Future<void> logIn({
    required String username,
    required String password,
  }) async {
    try {
      var client = await oauth2.resourceOwnerPasswordGrant(
          _authorizationEndpoint, username, password,
          identifier: _identifier, secret: _secret);
      _credentials = client.credentials;
      await _storage.write(
          key: _keyStorage, value: client.credentials.toJson());
      client.close();
      return;
    } catch (ex) {
      throw AplicationErrors.auth.unauthenticatedAccess;
    }
  }

  Future<String> getAuthorizationHeader() async {
    var credentials = await getCredentials();
    return "Bearer ${credentials.accessToken}";
  }

  Future<String> getToken() async {
    var credentials = await getCredentials();
    return credentials.accessToken;
  }

  Future<oauth2.Credentials> getCredentials() async {
    await _recoverFromStorageCredentials();
    if (_credentials!.isExpired) {
      if (_credentials!.canRefresh == false) {
        throw AplicationErrors.auth.unauthenticatedAccess;
      }
      _credentials =
          await _credentials!.refresh(identifier: _identifier, secret: _secret);
      await _storage.write(key: _keyStorage, value: _credentials!.toJson());
    }
    return _credentials!;
  }

  Future<List<String>> getCompaniesIds() async {
    var accessTokenString = await getToken();
    var accessToken = JwtDecoder.decode(accessTokenString);
    var companies = accessToken["companies"] as List<dynamic>;
    return companies.map((el) => el.toString()).toList();
  }

  Future _recoverFromStorageCredentials() async {
    if (_credentials == null) {
      var credentialsJson = await _storage.read(key: _keyStorage);
      if (credentialsJson == null) {
        throw AplicationErrors.auth.unauthenticatedAccess;
      }
      _credentials = oauth2.Credentials.fromJson(credentialsJson);
    }
  }

  Future<void> logOut() async {
    try {
      var credentials = await getCredentials();
      var client = oauth2.Client(credentials);
      Map<String, String> authData = {
        "client_id": _identifier,
        "client_secret": _secret,
        "refresh_token": credentials.refreshToken!
      };
      await client.post(_endSessionEndpoint, body: authData);
      client.close();
    } catch (_) {
    } finally {
      _storage.delete(key: _keyStorage);
      _credentials = null;
    }
  }
}
