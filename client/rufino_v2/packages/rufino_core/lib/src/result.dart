sealed class Result<T> {
  const Result();

  const factory Result.success(T value) = Success<T>;
  const factory Result.error(Object error, [StackTrace? stackTrace]) =
      Failure<T>;

  bool get isSuccess => this is Success<T>;
  bool get isError => this is Failure<T>;

  T? get valueOrNull => switch (this) {
        Success(:final value) => value,
        Failure() => null,
      };

  Object? get errorOrNull => switch (this) {
        Success() => null,
        Failure(:final error) => error,
      };

  R fold<R>({
    required R Function(T value) onSuccess,
    required R Function(Object error, StackTrace? stackTrace) onError,
  }) =>
      switch (this) {
        Success(:final value) => onSuccess(value),
        Failure(:final error, :final stackTrace) => onError(error, stackTrace),
      };
}

final class Success<T> extends Result<T> {
  const Success(this.value);
  final T value;
}

final class Failure<T> extends Result<T> {
  const Failure(this.error, [this.stackTrace]);
  final Object error;
  final StackTrace? stackTrace;
}
