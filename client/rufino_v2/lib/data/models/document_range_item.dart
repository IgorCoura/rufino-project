/// Request body item for batch document operations (generate/download range).
///
/// Groups a [documentId] with a list of [documentUnitIds] to be processed
/// together in a single API call.
class DocumentRangeItem {
  const DocumentRangeItem({
    required this.documentId,
    required this.documentUnitIds,
  });

  /// The document to which the units belong.
  final String documentId;

  /// The unit ids to include in the batch operation.
  final List<String> documentUnitIds;

  /// Serialises this item for the range API body.
  Map<String, dynamic> toJson() => {
        'documentId': documentId,
        'documentUnitIds': documentUnitIds,
      };
}
