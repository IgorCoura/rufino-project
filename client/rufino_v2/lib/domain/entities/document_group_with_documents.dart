import 'employee_document.dart';

/// A document group with its associated employee documents.
///
/// Represents the hierarchical view returned by the
/// `withdocuments/{employeeId}` endpoint, where each group contains
/// the employee's required documents that belong to it.
class DocumentGroupWithDocuments {
  const DocumentGroupWithDocuments({
    required this.id,
    required this.name,
    required this.description,
    required this.statusId,
    required this.statusName,
    this.documents = const [],
  });

  /// Unique identifier for this document group.
  final String id;

  /// Human-readable name of the group.
  final String name;

  /// Detailed description of the group purpose.
  final String description;

  /// The aggregate compliance status id of all documents in this group.
  ///
  /// Same three-valued scale the employee list uses (0=Okay, 1=A Vencer,
  /// 2=Requer Atenção) — the server derives both from one rule, so a group can
  /// never disagree with the employee it belongs to.
  final String statusId;

  /// The aggregate status display name.
  final String statusName;

  /// The employee documents belonging to this group.
  final List<EmployeeDocument> documents;

  /// Display label for the group status.
  String get groupStatusLabel => switch (statusId) {
        '0' => 'OK',
        '1' => 'A Vencer',
        '2' => 'Requer Atenção',
        _ => statusName.isNotEmpty ? statusName : statusId,
      };

  /// Returns a copy with [documents] replaced.
  DocumentGroupWithDocuments copyWithDocuments(
      List<EmployeeDocument> documents) {
    return DocumentGroupWithDocuments(
      id: id,
      name: name,
      description: description,
      statusId: statusId,
      statusName: statusName,
      documents: documents,
    );
  }
}
