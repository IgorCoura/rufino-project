import '../../core/result.dart';
import '../entities/require_document.dart';
import '../entities/selection_option.dart';

/// Contract for accessing and mutating document requirement rules.
abstract class RequireDocumentRepository {
  /// Returns all require documents for the given [companyId].
  Future<Result<List<RequireDocument>>> getRequireDocuments(String companyId);

  /// Returns the require document identified by [requireDocumentId] within [companyId].
  Future<Result<RequireDocument>> getRequireDocumentById(
      String companyId, String requireDocumentId);

  /// Creates a new require document and returns the generated id.
  Future<Result<String>> createRequireDocument(
    String companyId, {
    required String name,
    required String description,
    required List<String> associationIds,
    required int associationTypeId,
    required List<String> documentTemplateIds,
    required List<ListenEventPayload> listenEvents,
  });

  /// Updates an existing require document and returns its id.
  Future<Result<String>> updateRequireDocument(
    String companyId, {
    required String id,
    required String name,
    required String description,
    required List<String> associationIds,
    required int associationTypeId,
    required List<String> documentTemplateIds,
    required List<ListenEventPayload> listenEvents,
  });

  /// Returns the available association type options (Role, Workplace).
  Future<Result<List<SelectionOption>>> getAssociationTypes(String companyId);

  /// Returns the available associations for the given [associationTypeId].
  Future<Result<List<SelectionOption>>> getAssociations(
      String companyId, String associationTypeId);

  /// Returns the available employee lifecycle events for the given [companyId].
  Future<Result<List<SelectionOption>>> getEvents(String companyId);

  /// Returns the available employee statuses for the given [companyId].
  Future<Result<List<SelectionOption>>> getStatuses(String companyId);

  /// Returns the available document templates (simplified) for the given [companyId].
  Future<Result<List<SelectionOption>>> getDocumentTemplates(String companyId);
}

/// Payload for a listen event when creating or updating a require document.
class ListenEventPayload {
  const ListenEventPayload({
    required this.eventId,
    required this.statusIds,
  });

  /// The event identifier.
  final int eventId;

  /// The status identifiers that trigger this event.
  final List<int> statusIds;

  /// Converts this payload to a JSON-serializable map.
  Map<String, dynamic> toJson() => {
        'eventId': eventId,
        'status': statusIds,
      };
}
