import 'dart:convert';

/// Recursively replaces values of PII-bearing keys with `[Filtered]`.
///
/// The set of sensitive keys is the union of common Brazilian identifiers
/// (CPF, RG, CNPJ), payroll fields, contact data, and authentication
/// secrets. Keys are matched case-insensitively against [_sensitiveKeys].
///
/// Returns a new map; the input is not mutated. Recursion is depth-limited
/// to [_maxDepth] so a maliciously nested payload cannot blow the stack.
Map<String, Object?> scrubJson(Map<String, Object?> input) {
  return _scrubMap(input, 0);
}

/// Default maximum length for HTTP response bodies forwarded to monitoring.
const int defaultMaxBodyChars = 16 * 1024;

/// Returns [body] with PII keys scrubbed (when JSON) and truncated to
/// [maxChars] characters.
///
/// When the body cannot be parsed as JSON it is returned verbatim, only
/// truncated. The decoded value is wrapped under a synthetic key so that
/// nested PII inside top-level lists or scalars is still scrubbed by
/// [scrubJson]'s recursive value walker.
String scrubAndTruncateBody(
  String body, {
  int maxChars = defaultMaxBodyChars,
}) {
  var result = body;
  try {
    final decoded = jsonDecode(body);
    final scrubbed = scrubJson(<String, Object?>{'_root': decoded});
    result = jsonEncode(scrubbed['_root']);
  } catch (_) {
    // Non-JSON body — leave as-is and just truncate.
  }
  if (result.length > maxChars) {
    return '${result.substring(0, maxChars)}…[truncated]';
  }
  return result;
}

const String _filteredPlaceholder = '[Filtered]';
const int _maxDepth = 16;

const Set<String> _sensitiveKeys = {
  // Brazilian personal identifiers
  'cpf',
  'rg',
  'cnpj',
  'pispasep',
  'pis',
  'pasep',
  'voterid',
  'tituloeleitor',
  'militarydocument',
  'documentomilitar',
  // Personal info
  'name',
  'fullname',
  'nome',
  'nomecompleto',
  'birthdate',
  'datanascimento',
  'motherName',
  'nomemae',
  'fatherName',
  'nomepai',
  // Contact
  'email',
  'phone',
  'telefone',
  'celular',
  'mobile',
  // Address
  'address',
  'endereco',
  'street',
  'rua',
  'cep',
  'zipcode',
  'postalcode',
  'complement',
  'complemento',
  'neighborhood',
  'bairro',
  // Payroll
  'salary',
  'salario',
  'salaryamount',
  'wage',
  'remuneration',
  'remuneracao',
  // Auth
  'password',
  'senha',
  'secret',
  'token',
  'accesstoken',
  'refreshtoken',
  'idtoken',
  'authorization',
  'apikey',
};

Map<String, Object?> _scrubMap(Map<String, Object?> map, int depth) {
  if (depth >= _maxDepth) return map;
  final out = <String, Object?>{};
  map.forEach((key, value) {
    if (_isSensitive(key)) {
      out[key] = _filteredPlaceholder;
    } else {
      out[key] = _scrubValue(value, depth + 1);
    }
  });
  return out;
}

Object? _scrubValue(Object? value, int depth) {
  if (depth >= _maxDepth) return value;
  if (value is Map<String, Object?>) {
    return _scrubMap(value, depth);
  }
  if (value is Map) {
    final copied = <String, Object?>{};
    value.forEach((k, v) {
      if (k is String) copied[k] = v;
    });
    return _scrubMap(copied, depth);
  }
  if (value is List) {
    return value.map((e) => _scrubValue(e, depth + 1)).toList();
  }
  return value;
}

bool _isSensitive(String key) {
  return _sensitiveKeys.contains(key.toLowerCase());
}
