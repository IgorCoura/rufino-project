/// Portuguese labels for the three document status scales the server exposes.
///
/// The API sends every status as `{id, name}`, where `name` is the smart enum
/// name in English (`"Pending"`, `"AwaitingSignature"`). Both halves are
/// translated here: the id is the primary key, and the name is a second key for
/// the same label — so a status whose id the client does not recognise still
/// renders in Portuguese instead of leaking the English name to the screen.
///
/// This is the single source of truth for these labels. Entities expose a
/// `statusLabel` getter delegating here; widgets never build the label
/// themselves and never render the raw `statusName`.
library;

/// Labels for `DocumentUnitStatus` (the only status the server writes).
const Map<String, String> _unitLabelsById = {
  '1': 'Pendente',
  '2': 'OK',
  '3': 'Obsoleto',
  '4': 'Inválido',
  '5': 'Requer Validação',
  '6': 'Não Aplicável',
  '7': 'Aguardando Assinatura',
  '8': 'A Vencer',
  '9': 'Vencido',
};

const Map<String, String> _unitLabelsByName = {
  'pending': 'Pendente',
  'ok': 'OK',
  'deprecated': 'Obsoleto',
  'invalid': 'Inválido',
  'requiresvalidation': 'Requer Validação',
  'notapplicable': 'Não Aplicável',
  'awaitingsignature': 'Aguardando Assinatura',
  'warning': 'A Vencer',
  'expired': 'Vencido',
};

/// Labels for `DocumentStatus` — the document-level status derived from its
/// units. Note id `1` is `RequiresDocument` here, not `Pending`: the two scales
/// share ids with different meanings, which is why they never share a map.
const Map<String, String> _documentLabelsById = {
  '1': 'Falta Entregar',
  '2': 'Requer Validação',
  '3': 'OK',
  '4': 'Obsoleto',
  '5': 'Aguardando Assinatura',
  '6': 'A Vencer',
  '7': 'Vencido',
};

const Map<String, String> _documentLabelsByName = {
  'requiresdocument': 'Falta Entregar',
  'requiresvalidation': 'Requer Validação',
  'ok': 'OK',
  'deprecated': 'Obsoleto',
  'awaitingsignature': 'Aguardando Assinatura',
  'warning': 'A Vencer',
  'expired': 'Vencido',
};

/// Labels for `EmployeeDocumentStatus`, the three-valued compliance rollup
/// shared by the employee, the document group and the required document.
const Map<String, String> _complianceLabelsById = {
  '0': 'OK',
  '1': 'A Vencer',
  '2': 'Requer Atenção',
};

const Map<String, String> _complianceLabelsByName = {
  'okay': 'OK',
  'warning': 'A Vencer',
  'requiresattention': 'Requer Atenção',
};

/// Resolves [id] then [name] against [byId]/[byName].
///
/// Returns the raw [name] when neither matches — a status the client has never
/// heard of is better shown as the server named it than hidden — and the [id]
/// when there is no name either.
String _resolve(
  String id,
  String name,
  Map<String, String> byId,
  Map<String, String> byName,
) {
  final byIdLabel = byId[id.trim()];
  if (byIdLabel != null) return byIdLabel;

  final byNameLabel = byName[name.trim().toLowerCase()];
  if (byNameLabel != null) return byNameLabel;

  return name.isNotEmpty ? name : id;
}

/// Portuguese label for a document unit status.
String documentUnitStatusLabel(String id, String name) =>
    _resolve(id, name, _unitLabelsById, _unitLabelsByName);

/// Portuguese label for a document status.
String documentStatusLabel(String id, String name) =>
    _resolve(id, name, _documentLabelsById, _documentLabelsByName);

/// Portuguese label for the three-valued compliance rollup.
String documentComplianceStatusLabel(String id, String name) =>
    _resolve(id, name, _complianceLabelsById, _complianceLabelsByName);
