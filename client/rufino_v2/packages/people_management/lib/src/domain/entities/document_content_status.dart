/// Whether the snapshot stored in a document unit still matches the
/// employee's current data.
///
/// The snapshot is taken when the unit's date is updated and is what the
/// generated PDF is built from, so it silently ages as the employee record
/// changes.
class DocumentContentStatus {
  /// Creates a [DocumentContentStatus].
  const DocumentContentStatus({
    required this.documentUnitId,
    required this.isOutdated,
    required this.checkFailed,
  });

  /// Identifier of the checked document unit.
  final String documentUnitId;

  /// Whether the stored snapshot diverges from the current data.
  final bool isOutdated;

  /// Whether the comparison could not be completed on the server.
  ///
  /// When true, [isOutdated] carries no information: a data block that failed
  /// to load is indistinguishable from one that changed.
  final bool checkFailed;

  /// Whether the user should be warned about this unit before generating.
  ///
  /// An inconclusive check never warns — nagging the user into overwriting a
  /// good snapshot because of a transient failure is worse than staying quiet.
  bool get needsWarning => isOutdated && !checkFailed;
}
