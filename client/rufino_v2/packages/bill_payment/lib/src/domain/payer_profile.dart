/// Wire values of the backend's `PayerKind` smart enum.
abstract final class PayerKinds {
  /// A natural person — CPF.
  static const String individual = 'Individual';

  /// A legal entity — CNPJ, and the only kind that supports CNPJ-root
  /// matching.
  static const String company = 'Company';

  /// The label to show for [kind].
  static String label(String kind) =>
      kind == individual ? 'Pessoa física' : 'Pessoa jurídica';
}

/// A fiscal document of the payer.
class PayerTaxId {
  /// Creates the document record.
  const PayerTaxId({required this.value, required this.kind});

  /// The formatted CPF/CNPJ.
  final String value;

  /// `CPF` or `CNPJ`.
  final String kind;
}

/// The tenant's payer profile — one per tenant.
///
/// This cadastro is a functional prerequisite: without it there is no
/// password derivation for locked PDFs and no payer check.
class PayerProfile {
  /// Creates the profile record.
  const PayerProfile({
    required this.id,
    required this.kind,
    required this.legalName,
    required this.primaryTaxId,
    required this.primaryTaxIdKind,
    required this.additionalTaxIds,
    required this.matchByCnpjRoot,
    required this.canSchedulePayments,
  });

  /// The profile's id.
  final String id;

  /// One of [PayerKinds].
  final String kind;

  /// The legal name.
  final String legalName;

  /// The main formatted CPF/CNPJ.
  final String primaryTaxId;

  /// `CPF` or `CNPJ`.
  final String primaryTaxIdKind;

  /// Extra documents (branches, the MEI's own CPF, a spouse).
  final List<PayerTaxId> additionalTaxIds;

  /// Whether routing may match by the CNPJ root — companies only.
  final bool matchByCnpjRoot;

  /// Whether a payment provider account is linked. The account reference
  /// itself never leaves the server.
  final bool canSchedulePayments;

  /// Whether the CNPJ-root toggle applies to this profile.
  bool get supportsCnpjRootMatching => kind == PayerKinds.company;
}
