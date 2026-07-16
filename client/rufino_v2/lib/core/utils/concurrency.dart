/// Utilities for running asynchronous work with a bounded level of
/// concurrency.
///
/// The batch document/download flows fan out many independent async
/// operations (per-template API calls, per-page OCR, per-file text
/// extraction). Running them all at once can overwhelm the network stack,
/// the OCR engine, or device memory; running them one-by-one wastes time.
/// [mapWithConcurrency] sits between those extremes: it keeps at most
/// [concurrency] tasks in flight while preserving input order in the result.
library;

/// Applies [task] to each element of [items] running at most [concurrency]
/// tasks concurrently, and returns the results in the original input order.
///
/// A worker pool of `min(concurrency, length)` workers pulls the next index
/// off a shared cursor and awaits [task]. Because reading and advancing the
/// cursor happen synchronously before any `await`, no two workers ever pick
/// the same element (Dart's single-threaded event loop guarantees this).
///
/// The returned list has the same length and ordering as [items] — result
/// `i` is always `task(items[i])`, regardless of completion order.
///
/// Error semantics mirror [Future.wait]: if any [task] throws, the returned
/// future completes with that error once the in-flight tasks settle. Callers
/// that need partial results should have [task] catch internally and return a
/// result wrapper (e.g. `Result`) instead of throwing.
///
/// Throws [ArgumentError] when [concurrency] is less than 1.
Future<List<R>> mapWithConcurrency<T, R>(
  Iterable<T> items,
  Future<R> Function(T item) task, {
  int concurrency = 4,
}) async {
  if (concurrency < 1) {
    throw ArgumentError.value(
      concurrency,
      'concurrency',
      'must be greater than or equal to 1',
    );
  }

  final list = items.toList();
  if (list.isEmpty) return <R>[];

  final results = List<R?>.filled(list.length, null);
  var cursor = 0;

  Future<void> worker() async {
    while (true) {
      final index = cursor;
      if (index >= list.length) break;
      cursor++;
      results[index] = await task(list[index]);
    }
  }

  final workerCount = concurrency < list.length ? concurrency : list.length;
  await Future.wait([for (var w = 0; w < workerCount; w++) worker()]);

  return results.cast<R>();
}
