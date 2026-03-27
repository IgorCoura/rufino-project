import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_vote_id.dart';

void main() {
  group('EmployeeVoteId computed properties', () {
    test('hasNumber returns true when number is not empty', () {
      const voteId = EmployeeVoteId(number: '012345678901');
      expect(voteId.hasNumber, isTrue);
    });

    test('hasNumber returns false when number is empty', () {
      const voteId = EmployeeVoteId(number: '');
      expect(voteId.hasNumber, isFalse);
    });
  });

  group('EmployeeVoteId.isVoteIdValid', () {
    test('returns false for wrong length', () {
      expect(EmployeeVoteId.isVoteIdValid('123'), isFalse);
    });

    test('returns false for all-same-digit numbers', () {
      expect(EmployeeVoteId.isVoteIdValid('111111111111'), isFalse);
    });

    test('returns false for an invalid check digit', () {
      expect(EmployeeVoteId.isVoteIdValid('012345670000'), isFalse);
    });
  });

  group('EmployeeVoteId.validateNumber', () {
    test('returns error when empty', () {
      expect(EmployeeVoteId.validateNumber(null), isNotNull);
      expect(EmployeeVoteId.validateNumber(''), isNotNull);
    });

    test('returns error for wrong length', () {
      expect(EmployeeVoteId.validateNumber('12345'), isNotNull);
    });
  });

  group('EmployeeVoteId.formatted', () {
    test('formats 12-digit number as XXXX.XXXX.XXXX', () {
      const voteId = EmployeeVoteId(number: '012345678901');
      expect(voteId.formatted, '0123.4567.8901');
    });

    test('returns raw value when digit count is not 12', () {
      const voteId = EmployeeVoteId(number: '12345');
      expect(voteId.formatted, '12345');
    });
  });
}
