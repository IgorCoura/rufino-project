/// Maps document signing option IDs to their Portuguese display names.
///
/// Matches the legacy app's `DocumentSigningOptions.conversionMapIntToString`.
class SigningOption {
  SigningOption._();

  /// Maps option IDs to their Portuguese display names.
  static const displayNames = {
    '1': 'Assinatura Física',
    '2': 'Assinatura Digital e Whatsapp',
    '3': 'Assinatura Digital e Selfie',
    '4': 'Assinatura Digital e SMS',
    '5': 'Apenas SMS',
    '6': 'Apenas Whatsapp',
  };

  /// Returns the display name for [id], or `"Não informado"` when unknown.
  static String labelForId(String id) =>
      displayNames[id] ?? 'Não informado';
}
