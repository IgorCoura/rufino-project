import '../../core/result.dart';
import '../entities/document_group.dart';
import '../entities/document_group_with_documents.dart';
import '../entities/document_group_with_templates.dart';

/// Contract for accessing and mutating document group data.
///
/// Document groups are company-scoped collections used to categorize
/// document templates. All operations require a [companyId] to scope
/// the data to the correct tenant.
abstract class DocumentGroupRepository {
  /// Returns all document groups for the given [companyId].
  Future<Result<List<DocumentGroup>>> getDocumentGroups(String companyId);

  /// Returns all document groups with their nested templates for [companyId].
  Future<Result<List<DocumentGroupWithTemplates>>>
      getDocumentGroupsWithTemplates(String companyId);

  /// Returns all document groups with their employee documents for
  /// the given [companyId] and [employeeId].
  Future<Result<List<DocumentGroupWithDocuments>>>
      getDocumentGroupsWithDocuments(String companyId, String employeeId);

  /// Creates a new document group and returns the generated id.
  Future<Result<String>> createDocumentGroup(
    String companyId, {
    required String name,
    required String description,
  });

  /// Updates an existing document group identified by [id] and returns its id.
  Future<Result<String>> updateDocumentGroup(
    String companyId, {
    required String id,
    required String name,
    required String description,
  });
}
