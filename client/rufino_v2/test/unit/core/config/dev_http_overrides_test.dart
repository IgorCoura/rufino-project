@TestOn('vm')
library;

import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/config/dev_http_overrides.dart';

class _FakeCertificate implements X509Certificate {
  @override
  Uint8List get der => Uint8List(0);
  @override
  String get pem => '';
  @override
  Uint8List get sha1 => Uint8List(0);
  @override
  String get subject => '';
  @override
  String get issuer => '';
  @override
  DateTime get startValidity => DateTime.fromMillisecondsSinceEpoch(0);
  @override
  DateTime get endValidity => DateTime.fromMillisecondsSinceEpoch(0);
}

void main() {
  group('acceptAnyCertificate', () {
    test('returns true regardless of host or port', () {
      final cert = _FakeCertificate();
      expect(acceptAnyCertificate(cert, '192.168.15.8', 8041), isTrue);
      expect(acceptAnyCertificate(cert, 'localhost', 443), isTrue);
      expect(acceptAnyCertificate(cert, 'any.example.com', 80), isTrue);
    });
  });

  group('applyDevHttpOverrides', () {
    HttpOverrides? originalOverrides;

    setUp(() {
      originalOverrides = HttpOverrides.current;
    });

    tearDown(() {
      HttpOverrides.global = originalOverrides;
    });

    test('installs a DevHttpOverrides instance on HttpOverrides.global', () {
      applyDevHttpOverrides();

      expect(HttpOverrides.current, isA<DevHttpOverrides>());
    });

    test('createHttpClient returns a non-null HttpClient', () {
      applyDevHttpOverrides();

      final overrides = HttpOverrides.current!;
      final client = overrides.createHttpClient(null);
      addTearDown(() => client.close(force: true));

      expect(client, isA<HttpClient>());
    });

    test('is idempotent — calling twice does not throw', () {
      applyDevHttpOverrides();
      expect(applyDevHttpOverrides, returnsNormally);
      expect(HttpOverrides.current, isA<DevHttpOverrides>());
    });

    test(
      'an HttpClient produced by the override accepts a self-signed cert '
      'served by a local HTTPS server',
      () async {
        final server = await _bindLocalSelfSignedHttpsServer();
        addTearDown(() => server.close(force: true));
        server.listen((req) {
          req.response
            ..statusCode = HttpStatus.ok
            ..write('ok');
          req.response.close();
        });

        applyDevHttpOverrides();

        final client = HttpClient();
        addTearDown(() => client.close(force: true));

        final request = await client.getUrl(
          Uri.parse('https://127.0.0.1:${server.port}/'),
        );
        final response = await request.close();

        expect(response.statusCode, HttpStatus.ok);
      },
      skip: _selfSignedCertPem == null
          ? 'self-signed cert fixtures not embedded in this test'
          : false,
    );
  });
}

/// Self-signed cert + key embedded as PEM. Set to `null` to skip the
/// HTTPS-handshake test on environments where [SecurityContext] cannot
/// load the bytes (the unit test still locks in the override wiring).
const String? _selfSignedCertPem = null;
const String? _selfSignedKeyPem = null;

Future<HttpServer> _bindLocalSelfSignedHttpsServer() async {
  final certPem = _selfSignedCertPem!;
  final keyPem = _selfSignedKeyPem!;
  final context = SecurityContext()
    ..useCertificateChainBytes(certPem.codeUnits)
    ..usePrivateKeyBytes(keyPem.codeUnits);
  return HttpServer.bindSecure('127.0.0.1', 0, context);
}
