import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:rufino_v2/core/network/session_aware_http_client.dart';

void main() {
  group('SessionAwareHttpClient', () {
    http.Client innerWithStatus(int statusCode) {
      return http_testing.MockClient(
        (request) async => http.Response('', statusCode),
      );
    }

    test('invokes onSessionInvalid when a response comes back 401', () async {
      var invoked = 0;
      final client = SessionAwareHttpClient(
        innerWithStatus(401),
        onSessionInvalid: () => invoked++,
      );

      await client.get(Uri.parse('https://api.test/employees'));

      expect(invoked, 1);
    });

    test('does not invoke onSessionInvalid for a successful response',
        () async {
      var invoked = 0;
      final client = SessionAwareHttpClient(
        innerWithStatus(200),
        onSessionInvalid: () => invoked++,
      );

      await client.get(Uri.parse('https://api.test/employees'));

      expect(invoked, 0);
    });

    test('does not invoke onSessionInvalid for a 403 permission denial',
        () async {
      var invoked = 0;
      final client = SessionAwareHttpClient(
        innerWithStatus(403),
        onSessionInvalid: () => invoked++,
      );

      await client.get(Uri.parse('https://api.test/employees'));

      expect(invoked, 0);
    });

    test('returns the inner response untouched', () async {
      final client = SessionAwareHttpClient(
        http_testing.MockClient(
          (request) async => http.Response('payload', 401),
        ),
        onSessionInvalid: () {},
      );

      final response =
          await client.get(Uri.parse('https://api.test/employees'));

      expect(response.statusCode, 401);
      expect(response.body, 'payload');
    });
  });
}
