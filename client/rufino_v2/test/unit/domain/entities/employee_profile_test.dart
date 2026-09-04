import 'package:flutter_test/flutter_test.dart';
import 'package:people_management/people_management.dart';

void main() {
  group('EmployeeProfile computed properties', () {
    const profile = EmployeeProfile(
      id: '1',
      name: 'João Silva',
      registration: '001',
      status: EmployeeStatus.active,
      roleId: 'role-1',
      workplaceId: 'wp-1',
      documentSigningOptionsId: 'opt-1',
    );

    const emptyProfile = EmployeeProfile(
      id: '2',
      name: 'Maria',
      registration: '002',
      status: EmployeeStatus.inactive,
      roleId: '',
      workplaceId: '',
    );

    test('canMarkAsInactive returns true only for pending employees', () {
      expect(
        profile.copyWith(status: EmployeeStatus.pending).canMarkAsInactive,
        isTrue,
      );
      expect(profile.canMarkAsInactive, isFalse);
      expect(emptyProfile.canMarkAsInactive, isFalse);
    });

    test('hasRole returns true when roleId is not empty', () {
      expect(profile.hasRole, isTrue);
      expect(emptyProfile.hasRole, isFalse);
    });

    test('hasWorkplace returns true when workplaceId is not empty', () {
      expect(profile.hasWorkplace, isTrue);
      expect(emptyProfile.hasWorkplace, isFalse);
    });

    test('hasSigningOptions returns true when id is not empty', () {
      expect(profile.hasSigningOptions, isTrue);
      expect(emptyProfile.hasSigningOptions, isFalse);
    });

    test(
        'canReceiveDocumentsToSign returns true only for digital signing '
        'options', () {
      expect(
        profile.copyWith(documentSigningOptionsId: '2').canReceiveDocumentsToSign,
        isTrue,
      );
      expect(
        profile.copyWith(documentSigningOptionsId: '6').canReceiveDocumentsToSign,
        isTrue,
      );
    });

    test('canReceiveDocumentsToSign returns false for physical signature', () {
      expect(
        profile.copyWith(documentSigningOptionsId: '1').canReceiveDocumentsToSign,
        isFalse,
      );
    });

    test('canReceiveDocumentsToSign returns false when option is not set', () {
      expect(emptyProfile.canReceiveDocumentsToSign, isFalse);
    });

    test('isActive returns true only for active status', () {
      expect(profile.isActive, isTrue);
      expect(emptyProfile.isActive, isFalse);
    });

    test('isInactive returns true only for inactive status', () {
      expect(emptyProfile.isInactive, isTrue);
      expect(profile.isInactive, isFalse);
    });

    test('copyWith overrides only the specified fields', () {
      final updated = profile.copyWith(name: 'João Carlos');
      expect(updated.name, 'João Carlos');
      expect(updated.id, profile.id);
      expect(updated.status, profile.status);
    });
  });
}
