import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/permission_model.dart';
import 'package:rufino_v2/domain/entities/permission.dart';

void main() {
  group('PermissionModel', () {
    test('fromJson creates a model from a valid JSON map', () {
      final json = {
        'resource': 'employee',
        'scopes': ['create', 'view', 'edit'],
      };

      final model = PermissionModel.fromJson(json);

      expect(model.resource, 'employee');
      expect(model.scopes, ['create', 'view', 'edit']);
    });

    test('toJson serializes the model to a JSON map', () {
      const model = PermissionModel(
        resource: 'document',
        scopes: ['upload', 'download'],
      );

      final json = model.toJson();

      expect(json['resource'], 'document');
      expect(json['scopes'], ['upload', 'download']);
    });

    test('fromEntity creates a model from a Permission entity', () {
      const entity = Permission(
        resource: 'company',
        scopes: ['create', 'view'],
      );

      final model = PermissionModel.fromEntity(entity);

      expect(model.resource, 'company');
      expect(model.scopes, ['create', 'view']);
    });

    test('toEntity converts the model to a Permission entity', () {
      const model = PermissionModel(
        resource: 'workplace',
        scopes: ['edit'],
      );

      final entity = model.toEntity();

      expect(entity.resource, 'workplace');
      expect(entity.hasScope('edit'), isTrue);
      expect(entity.hasScope('create'), isFalse);
    });

    test('round-trip from entity to JSON and back preserves data', () {
      const original = Permission(
        resource: 'department',
        scopes: ['create', 'edit', 'view'],
      );

      final json = PermissionModel.fromEntity(original).toJson();
      final restored = PermissionModel.fromJson(json).toEntity();

      expect(restored.resource, original.resource);
      expect(restored.scopes, original.scopes);
    });

    test('handles empty scopes list', () {
      final json = {
        'resource': 'role',
        'scopes': <String>[],
      };

      final model = PermissionModel.fromJson(json);

      expect(model.scopes, isEmpty);
      expect(model.toEntity().scopes, isEmpty);
    });
  });
}
