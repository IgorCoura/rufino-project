import 'dart:convert';

import 'package:http/http.dart' as http;

import 'permission.dart';
import 'permission_exception.dart';

/// Fetches the current user's permissions from Keycloak Authorization
/// Services for a single resource server.
///
/// Uses the UMA (User-Managed Access) grant type to request an RPT
/// (Requesting Party Token) with `response_mode=permissions`, which returns
/// all resource/scope pairs the user is authorized for on [audience].
///
/// One instance serves one [audience]. The app creates as many as it has
/// resource servers — permissions granted on `people-management-api` say
/// nothing about `tenant-management-api`, and mixing them would let a
/// resource name granted on one server unlock a screen guarded by the other.
class PermissionApiService {
  /// Creates the service for the resource server named [audience].
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

  /// Fetches all permissions for the current user on [audience].
  ///
  /// Returns one [Permission] per authorized resource, each containing the
  /// scopes granted on that resource.
  ///
  /// A **403** answer means the user holds no permission at all on this
  /// resource server, which is a normal outcome — not every person is a
  /// platform operator — so it resolves to an empty list. Treating it as a
  /// failure would put every user of the other product in front of an error
  /// screen the moment a second audience was introduced.
  ///
  /// Throws [PermissionFetchException] for any other non-2xx status.
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

    if (response.statusCode == 403) return const [];

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
