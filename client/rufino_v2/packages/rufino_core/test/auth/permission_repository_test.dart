import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../fakes/fakes.dart';

/// The seam between the UMA call, the local cache and the error reporter.
///
/// Everything that leaves this layer is a [Result]; nothing throws past it.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late RecordingErrorReporter reporter;
  late PermissionCacheService cache;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    reporter = RecordingErrorReporter();
    cache = PermissionCacheService(
      prefs: await SharedPreferences.getInstance(),
    );
  });

  PermissionRepositoryImpl repositoryAnswering(int status, {String body = ''}) {
    return PermissionRepositoryImpl(
      permissionApiService: PermissionApiService(
        client: FakeHttpClient((_) async => http.Response(body, status)),
        tokenEndpoint: Uri.parse('https://keycloak.test/token'),
        getAccessToken: () async => 'token',
        audience: 'people-management-api',
      ),
      permissionCacheService: cache,
      reporter: reporter,
    );
  }

  group('PermissionRepositoryImpl fetching', () {
    test('returns the permissions the identity provider granted', () async {
      final repository = repositoryAnswering(
        200,
        body: jsonEncode([
          {
            'rsname': 'employee',
            'scopes': ['view'],
          },
        ]),
      );

      final result = await repository.fetchPermissions();

      expect(result.isSuccess, isTrue);
      expect(result.valueOrNull!.single.resource, 'employee');
    });

    test('returns an empty success when the user has no access to this '
        'audience', () async {
      final repository = repositoryAnswering(403);

      final result = await repository.fetchPermissions();

      expect(result.isSuccess, isTrue);
      expect(result.valueOrNull, isEmpty);
      expect(reporter.offered, isEmpty);
    });

    test('returns a failure and reports it when the request fails', () async {
      final repository = repositoryAnswering(500);

      final result = await repository.fetchPermissions();

      expect(result.isError, isTrue);
      expect(result.errorOrNull, isA<PermissionFetchException>());
      expect(reporter.captured, hasLength(1));
    });

    test('wraps an unexpected failure so callers only ever see a permission '
        'exception', () async {
      final repository = repositoryAnswering(200, body: '{"not":"a list"}');

      final result = await repository.fetchPermissions();

      expect(result.isError, isTrue);
      expect(result.errorOrNull, isA<PermissionFetchException>());
    });

    test('never throws past the repository boundary', () async {
      final repository = repositoryAnswering(200, body: 'not json');

      await expectLater(repository.fetchPermissions(), completes);
    });
  });

  group('PermissionRepositoryImpl caching', () {
    test('answers null before anything has been cached', () async {
      final repository = repositoryAnswering(200);

      expect(await repository.getCachedPermissions(), isNull);
    });

    test('returns what it previously cached', () async {
      final repository = repositoryAnswering(200);

      await repository.cachePermissions([
        grant('employee', ['view', 'edit']),
      ]);
      final cached = await repository.getCachedPermissions();

      expect(cached!.single.resource, 'employee');
      expect(cached.single.scopes, ['view', 'edit']);
    });

    test('forgets the cached permissions when asked to clear them', () async {
      final repository = repositoryAnswering(200);
      await repository.cachePermissions([
        grant('employee', ['view']),
      ]);

      await repository.clearCachedPermissions();

      expect(await repository.getCachedPermissions(), isNull);
    });
  });
}
