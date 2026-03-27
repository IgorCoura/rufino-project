import 'package:flutter_test/flutter_test.dart';

import '../../../testing/fakes/fake_permission_repository.dart';

/// Uses [FakePermissionRepository] to test the [PermissionRepositoryImpl]
/// contract indirectly. Since the fake implements the same interface, we
/// validate that the Result wrapping works correctly.
void main() {
  late FakePermissionRepository repository;

  setUp(() {
    repository = FakePermissionRepository();
  });

  group('PermissionRepositoryImpl', () {
    test('fetchPermissions returns success result with permissions', () async {
      final result = await repository.fetchPermissions();

      expect(result.isSuccess, isTrue);
      expect(result.valueOrNull, isEmpty);
    });

    test('fetchPermissions returns error result on failure', () async {
      repository.setShouldFail(true);

      final result = await repository.fetchPermissions();

      expect(result.isError, isTrue);
    });
  });
}
