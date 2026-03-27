import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_contact.dart';

void main() {
  group('EmployeeContact', () {
    test('hasPhone returns true when cellphone is not empty', () {
      const contact = EmployeeContact(cellphone: '11999998888', email: '');
      expect(contact.hasPhone, isTrue);
    });

    test('hasEmail returns true when email is not empty', () {
      const contact = EmployeeContact(cellphone: '', email: 'a@b.com');
      expect(contact.hasEmail, isTrue);
    });

    test('isEmpty returns true when both fields are empty', () {
      const contact = EmployeeContact(cellphone: '', email: '');
      expect(contact.isEmpty, isTrue);
    });

    test('isEmpty returns false when any field is filled', () {
      const contact = EmployeeContact(cellphone: '123', email: '');
      expect(contact.isEmpty, isFalse);
    });

    test('copyWith creates a new instance with overridden fields', () {
      const contact = EmployeeContact(cellphone: '123', email: 'a@b.com');
      final updated = contact.copyWith(email: 'new@mail.com');
      expect(updated.cellphone, '123');
      expect(updated.email, 'new@mail.com');
    });
  });

  group('EmployeeContact.validatePhone', () {
    test('returns null when value is null or empty (optional field)', () {
      expect(EmployeeContact.validatePhone(null), isNull);
      expect(EmployeeContact.validatePhone(''), isNull);
      expect(EmployeeContact.validatePhone('   '), isNull);
    });

    test('returns null for valid 10-digit phone', () {
      expect(EmployeeContact.validatePhone('1134567890'), isNull);
    });

    test('returns null for valid 11-digit phone', () {
      expect(EmployeeContact.validatePhone('11987654321'), isNull);
    });

    test('returns null for phone with mask characters', () {
      expect(EmployeeContact.validatePhone('(11) 98765-4321'), isNull);
    });

    test('returns error for phone with wrong digit count', () {
      expect(EmployeeContact.validatePhone('123456'), isNotNull);
      expect(EmployeeContact.validatePhone('123456789012'), isNotNull);
    });
  });

  group('EmployeeContact.validateEmail', () {
    test('returns null when value is null or empty (optional field)', () {
      expect(EmployeeContact.validateEmail(null), isNull);
      expect(EmployeeContact.validateEmail(''), isNull);
    });

    test('returns null for valid email', () {
      expect(EmployeeContact.validateEmail('user@example.com'), isNull);
      expect(EmployeeContact.validateEmail('user@sub.domain.com'), isNull);
    });

    test('returns error for invalid email format', () {
      expect(EmployeeContact.validateEmail('notanemail'), isNotNull);
      expect(EmployeeContact.validateEmail('user@'), isNotNull);
      expect(EmployeeContact.validateEmail('@domain.com'), isNotNull);
    });
  });

  group('EmployeeContact.formattedPhone', () {
    test('returns empty string when cellphone is empty', () {
      const contact = EmployeeContact(cellphone: '', email: '');
      expect(contact.formattedPhone, '');
    });

    test('formats 11-digit phone as +55 XX XXXXX-XXXX', () {
      const contact = EmployeeContact(cellphone: '11968608425', email: '');
      expect(contact.formattedPhone, '+55 11 96860-8425');
    });

    test('formats 10-digit phone as +55 XX XXXX-XXXX', () {
      const contact = EmployeeContact(cellphone: '1134567890', email: '');
      expect(contact.formattedPhone, '+55 11 3456-7890');
    });

    test('returns raw digits for unexpected length', () {
      const contact = EmployeeContact(cellphone: '12345', email: '');
      expect(contact.formattedPhone, '12345');
    });
  });
}
