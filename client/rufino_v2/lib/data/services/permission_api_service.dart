import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/errors/permission_exception.dart';
import '../../domain/entities/permission.dart';

/// Fetches the current user's permissions from Keycloak Authorization Services.
///
/// Uses the UMA (User-Managed Access) grant type to request an RPT
/// (Requesting Party Token) with `response_mode=permissions`, which returns
/// all resource/scope pairs the user is authorized for.
class PermissionApiService {
  PermissionApiService({
    required this.client,
    required this.tokenEndpoint,
    required this.getAccessToken,
    required this.audience,
  });

  final http.Client client;

  /// The Keycloak token endpoint URL (same as the OAuth2 authorization
  /// endpoint used for login).
  final Uri tokenEndpoint;

  /// Callback that returns the current user's raw access token.
  final Future<String> Function() getAccessToken;

  /// The Keycloak resource server (client) ID, e.g. `"people-management-api"`.
  final String audience;

  /// Fetches all permissions for the current user.
  ///
  /// Returns one [Permission] per authorized resource, each containing the
  /// scopes granted on that resource.
  ///
  /// Throws [PermissionFetchException] when the request fails.
  Future<List<Permission>> fetchPermissions() async {
    final accessToken = await getAccessToken();

    final response = await client.post(
      tokenEndpoint,
      headers: {
        'Authorization': 'Bearer $accessToken',
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      body: {
        'grant_type': 'urn:ietf:params:oauth:grant-type:uma-ticket',
        'audience': audience,
        'response_mode': 'permissions',
      },
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw PermissionFetchException(
        'HTTP ${response.statusCode}: ${response.reasonPhrase}',
      );
    }

    final List<dynamic> json = jsonDecode(response.body) as List<dynamic>;

    return json.map((entry) {
      final map = entry as Map<String, dynamic>;
      final resource = map['rsname'] as String;
      final rawScopes = map['scopes'] as List<dynamic>?;
      final scopes =
          rawScopes?.map((s) => s as String).toList() ?? const <String>[];
      return Permission(resource: resource, scopes: scopes);
    }).toList();
  }
}
