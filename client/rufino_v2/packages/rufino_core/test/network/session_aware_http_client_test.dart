import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

/// The single choke point where an expired session is noticed.
///
/// A false positive logs a working session out; a false negative leaves the
/// user staring at empty screens. Both directions are asserted.
void main() {
  final url = Uri.parse('https://api.test/v1/bills');

  group('SessionAwareHttpClient', () {
    test('invokes the session callback when a response comes back 401',
        () async {
      var invocations = 0;
      final client = SessionAwareHttpClient(
        FakeHttpClient.status(401),
        onSessionInvalid: () => invocations++,
      );

      await client.get(url);

      expect(invocations, 1);
    });

    test('invokes the callback once per 401, not once per client', () async {
      var invocations = 0;
      final client = SessionAwareHttpClient(
        FakeHttpClient.status(401),
        onSessionInvalid: () => invocations++,
      );

      await client.get(url);
      await client.get(url);
      await client.post(url, body: 'x');

      expect(invocations, 3);
    });

    test('leaves the session alone for every status other than 401', () async {
      for (final status in [200, 201, 204, 302, 400, 403, 404, 409, 429, 500]) {
        var invocations = 0;
        final client = SessionAwareHttpClient(
          FakeHttpClient.status(status),
          onSessionInvalid: () => invocations++,
        );

        await client.get(url);

        expect(
          invocations,
          0,
          reason: 'status $status must not invalidate the session',
        );
      }
    });

    test('does not log the user out on a 403 permission denial', () async {
      var invoked = false;
      final client = SessionAwareHttpClient(
        FakeHttpClient.status(403),
        onSessionInvalid: () => invoked = true,
      );

      await client.get(url);

      expect(invoked, isFalse);
    });

    test('returns the inner response untouched, body included', () async {
      final client = SessionAwareHttpClient(
        FakeHttpClient.status(401, body: 'unauthorized payload'),
        onSessionInvalid: () {},
      );

      final response = await client.get(url);

      expect(response.statusCode, 401);
      expect(response.body, 'unauthorized payload');
    });

    test('forwards the request to the inner client unchanged', () async {
      final inner = FakeHttpClient.status(200);
      final client = SessionAwareHttpClient(inner, onSessionInvalid: () {});

      await client.post(url, headers: {'x-test': 'yes'}, body: 'payload');

      final sent = inner.requests.single;
      expect(sent.method, 'POST');
      expect(sent.url, url);
      expect(sent.headers['x-test'], 'yes');
    });

    test('notifies before the caller sees the response', () async {
      final order = <String>[];
      final client = SessionAwareHttpClient(
        FakeHttpClient.status(401),
        onSessionInvalid: () => order.add('callback'),
      );

      await client.get(url);
      order.add('caller');

      expect(order, ['callback', 'caller']);
    });

    test('closes the inner client when it is closed', () {
      final inner = FakeHttpClient.status(200);
      final client = SessionAwareHttpClient(inner, onSessionInvalid: () {});

      client.close();

      expect(inner.closed, isTrue);
    });

    test('is usable anywhere an http.Client is expected', () {
      final client = SessionAwareHttpClient(
        FakeHttpClient.status(200),
        onSessionInvalid: () {},
      );

      expect(client, isA<http.Client>());
    });
  });
}
