import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

void main() {
  group('Permission', () {
    test('answers true only for a scope it was granted', () {
      const permission = Permission(
        resource: 'employee',
        scopes: ['view', 'create'],
      );

      expect(permission.hasScope('view'), isTrue);
      expect(permission.hasScope('create'), isTrue);
      expect(permission.hasScope('delete'), isFalse);
    });

    test('matches scopes exactly, without case folding', () {
      const permission = Permission(resource: 'employee', scopes: ['view']);

      expect(permission.hasScope('View'), isFalse);
    });

    test('grants nothing when the scope list is empty', () {
      const permission = Permission(resource: 'employee', scopes: []);

      expect(permission.hasScope('view'), isFalse);
      expect(permission.scopes, isEmpty);
    });

    test('exposes its scopes as a read-only view', () {
      const permission = Permission(resource: 'employee', scopes: ['view']);

      expect(() => permission.scopes.add('create'), throwsUnsupportedError);
    });
  });

  group('PermissionModel', () {
    test('reads the resource and scopes out of a cached JSON entry', () {
      final model = PermissionModel.fromJson({
        'resource': 'tenant',
        'scopes': ['view', 'edit'],
      });

      expect(model.resource, 'tenant');
      expect(model.scopes, ['view', 'edit']);
    });

    test('accepts an entry whose scope list is empty', () {
      final model = PermissionModel.fromJson({
        'resource': 'tenant',
        'scopes': <dynamic>[],
      });

      expect(model.scopes, isEmpty);
    });

    test('survives a full round trip through JSON without losing scopes', () {
      const original = Permission(
        resource: 'bill',
        scopes: ['view', 'approve', 'pay'],
      );

      final restored = PermissionModel.fromJson(
        PermissionModel.fromEntity(original).toJson(),
      ).toEntity();

      expect(restored.resource, original.resource);
      expect(restored.scopes, original.scopes);
    });

    test('serializes to the shape the cache stores', () {
      const permission = Permission(resource: 'bill', scopes: ['view']);

      expect(
        PermissionModel.fromEntity(permission).toJson(),
        {
          'resource': 'bill',
          'scopes': ['view'],
        },
      );
    });

    test('rejects an entry that is missing the resource name', () {
      expect(
        () => PermissionModel.fromJson({
          'scopes': ['view'],
        }),
        throwsA(isA<TypeError>()),
      );
    });
  });
}
