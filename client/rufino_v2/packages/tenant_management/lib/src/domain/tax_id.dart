import 'tenant_enums.dart';

/// A Brazilian tax identifier — CPF (11 digits) or CNPJ (14).
///
/// Mirrors the backend `TaxId` value object: the kind is deduced from the
/// length, the check digits are verified, and an all-same-digit document is
/// refused. Validating here is not a substitute for the server's invariant —
/// it is what keeps the user from filling a whole form to be told at the end
/// that the first field was wrong.
abstract final class TaxId {
  /// Whether [value] is a valid CPF, ignoring formatting.
  static bool isValidCpf(String value) {
    final digits = onlyDigits(value);
    if (digits.length != 11) return false;
    if (_allSameDigit(digits)) return false;

    final first = _checkDigit(digits, length: 9, startWeight: 10);
    if (first != int.parse(digits[9])) return false;

    final second = _checkDigit(digits, length: 10, startWeight: 11);
    return second == int.parse(digits[10]);
  }

  /// Whether [value] is a valid CNPJ, ignoring formatting.
  static bool isValidCnpj(String value) {
    final digits = onlyDigits(value);
    if (digits.length != 14) return false;
    if (_allSameDigit(digits)) return false;

    const firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    const secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    final first = _weightedCheckDigit(digits, firstWeights);
    if (first != int.parse(digits[12])) return false;

    final second = _weightedCheckDigit(digits, secondWeights);
    return second == int.parse(digits[13]);
  }

  /// Whether [value] is valid for the given tenant [kind].
  static bool isValidFor(String kind, String value) =>
      kind == TenantKinds.individual ? isValidCpf(value) : isValidCnpj(value);

  /// Returns [value] with every non-digit stripped.
  static String onlyDigits(String value) =>
      value.replaceAll(RegExp(r'[^\d]'), '');

  /// Formats [value] as `000.000.000-00` or `00.000.000/0000-00`.
  ///
  /// Returns the input unchanged when it has neither 11 nor 14 digits — a
  /// half-typed document is shown as typed, not mangled.
  static String format(String value) {
    final d = onlyDigits(value);
    if (d.length == 11) {
      return '${d.substring(0, 3)}.${d.substring(3, 6)}.'
          '${d.substring(6, 9)}-${d.substring(9)}';
    }
    if (d.length == 14) {
      return '${d.substring(0, 2)}.${d.substring(2, 5)}.${d.substring(5, 8)}/'
          '${d.substring(8, 12)}-${d.substring(12)}';
    }
    return value;
  }

  static bool _allSameDigit(String digits) =>
      digits.split('').every((c) => c == digits[0]);

  /// CPF check digit: weights run down from [startWeight] over [length]
  /// digits.
  static int _checkDigit(
    String digits, {
    required int length,
    required int startWeight,
  }) {
    var sum = 0;
    for (var i = 0; i < length; i++) {
      sum += int.parse(digits[i]) * (startWeight - i);
    }
    final rest = sum % 11;
    return rest < 2 ? 0 : 11 - rest;
  }

  /// CNPJ check digit: fixed weight table.
  static int _weightedCheckDigit(String digits, List<int> weights) {
    var sum = 0;
    for (var i = 0; i < weights.length; i++) {
      sum += int.parse(digits[i]) * weights[i];
    }
    final rest = sum % 11;
    return rest < 2 ? 0 : 11 - rest;
  }
}
