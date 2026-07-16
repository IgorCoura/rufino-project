import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/utils/concurrency.dart';

void main() {
  group('mapWithConcurrency', () {
    test('returns an empty list for empty input without invoking the task',
        () async {
      var calls = 0;
      final result = await mapWithConcurrency<int, int>(
        const <int>[],
        (i) async {
          calls++;
          return i;
        },
      );

      expect(result, isEmpty);
      expect(calls, 0);
    });

    test('preserves input order regardless of completion order', () async {
      // Later items finish first: item 0 waits longest, item 4 shortest.
      final result = await mapWithConcurrency<int, int>(
        [0, 1, 2, 3, 4],
        (i) async {
          await Future<void>.delayed(Duration(milliseconds: (5 - i) * 10));
          return i * 10;
        },
        concurrency: 5,
      );

      expect(result, [0, 10, 20, 30, 40]);
    });

    test('applies the task to every element exactly once', () async {
      final seen = <int>[];
      final result = await mapWithConcurrency<int, int>(
        [1, 2, 3, 4, 5, 6, 7],
        (i) async {
          seen.add(i);
          return i;
        },
        concurrency: 3,
      );

      expect(result, [1, 2, 3, 4, 5, 6, 7]);
      expect(seen..sort(), [1, 2, 3, 4, 5, 6, 7]);
    });

    test('never runs more than [concurrency] tasks at the same time',
        () async {
      var active = 0;
      var maxActive = 0;

      await mapWithConcurrency<int, int>(
        List.generate(12, (i) => i),
        (i) async {
          active++;
          if (active > maxActive) maxActive = active;
          await Future<void>.delayed(const Duration(milliseconds: 5));
          active--;
          return i;
        },
        concurrency: 3,
      );

      expect(maxActive, lessThanOrEqualTo(3));
      // With 12 items and a real delay, the pool should reach its ceiling.
      expect(maxActive, 3);
    });

    test('caps active workers at the item count when concurrency exceeds it',
        () async {
      var active = 0;
      var maxActive = 0;

      await mapWithConcurrency<int, int>(
        [1, 2],
        (i) async {
          active++;
          if (active > maxActive) maxActive = active;
          await Future<void>.delayed(const Duration(milliseconds: 5));
          active--;
          return i;
        },
        concurrency: 10,
      );

      expect(maxActive, 2);
    });

    test('runs strictly serially when concurrency is 1', () async {
      var active = 0;
      var maxActive = 0;

      final result = await mapWithConcurrency<int, int>(
        [1, 2, 3],
        (i) async {
          active++;
          if (active > maxActive) maxActive = active;
          await Future<void>.delayed(const Duration(milliseconds: 5));
          active--;
          return i;
        },
        concurrency: 1,
      );

      expect(maxActive, 1);
      expect(result, [1, 2, 3]);
    });

    test('propagates the error when a task throws', () async {
      expect(
        () => mapWithConcurrency<int, int>(
          [1, 2, 3],
          (i) async {
            if (i == 2) throw StateError('boom');
            return i;
          },
          concurrency: 2,
        ),
        throwsA(isA<StateError>()),
      );
    });

    test('throws ArgumentError when concurrency is below 1', () async {
      expect(
        () => mapWithConcurrency<int, int>([1], (i) async => i, concurrency: 0),
        throwsA(isA<ArgumentError>()),
      );
    });
  });
}
