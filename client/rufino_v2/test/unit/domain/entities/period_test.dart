import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/period.dart';

void main() {
  group('Period type getters', () {
    test('isDaily returns true only for typeId 1', () {
      const period = Period(typeId: 1, typeName: 'Daily', month: 3, year: 2026);
      expect(period.isDaily, isTrue);
      expect(period.isWeekly, isFalse);
      expect(period.isMonthly, isFalse);
      expect(period.isYearly, isFalse);
    });

    test('isWeekly returns true only for typeId 2', () {
      const period =
          Period(typeId: 2, typeName: 'Weekly', month: 3, year: 2026);
      expect(period.isWeekly, isTrue);
      expect(period.isDaily, isFalse);
    });

    test('isMonthly returns true only for typeId 3', () {
      const period =
          Period(typeId: 3, typeName: 'Monthly', month: 3, year: 2026);
      expect(period.isMonthly, isTrue);
    });

    test('isYearly returns true only for typeId 4', () {
      const period =
          Period(typeId: 4, typeName: 'Yearly', month: 1, year: 2026);
      expect(period.isYearly, isTrue);
    });
  });

  group('Period.formattedPeriod', () {
    test('formats daily period as dd/MM/yyyy', () {
      const period =
          Period(typeId: 1, typeName: 'Daily', day: 5, month: 3, year: 2026);
      expect(period.formattedPeriod, '05/03/2026');
    });

    test('formats weekly period with week number', () {
      const period =
          Period(typeId: 2, typeName: 'Weekly', week: 12, month: 3, year: 2026);
      expect(period.formattedPeriod, 'Semana 12 - 03/2026');
    });

    test('formats monthly period with abbreviated month name', () {
      const period =
          Period(typeId: 3, typeName: 'Monthly', month: 3, year: 2026);
      expect(period.formattedPeriod, 'Mar/2026');
    });

    test('formats yearly period as year only', () {
      const period =
          Period(typeId: 4, typeName: 'Yearly', month: 1, year: 2026);
      expect(period.formattedPeriod, '2026');
    });

    test('returns empty string for daily period without day', () {
      const period =
          Period(typeId: 1, typeName: 'Daily', month: 3, year: 2026);
      expect(period.formattedPeriod, '');
    });

    test('returns empty string for weekly period without week', () {
      const period =
          Period(typeId: 2, typeName: 'Weekly', month: 3, year: 2026);
      expect(period.formattedPeriod, '');
    });

    test('returns empty string for unknown typeId', () {
      const period =
          Period(typeId: 99, typeName: 'Unknown', month: 3, year: 2026);
      expect(period.formattedPeriod, '');
    });
  });
}
