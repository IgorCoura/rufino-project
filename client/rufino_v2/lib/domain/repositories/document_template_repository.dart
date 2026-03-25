import 'dart:typed_data';

import '../../core/result.dart';
import '../entities/document_template.dart';
import '../entities/selection_option.dart';

/// Contract for accessing and mutating document template data.
abstract class DocumentTemplateRepository {
  /// Returns all available document groups for the given [companyId].
  Future<Result<List<SelectionOption>>> getDocumentGroups(String companyId);

  /// Returns all available recover data type options for the given [companyId].
  Future<Result<List<SelectionOption>>> getRecoverDataTypes(String companyId);

  /// Returns the recover data models JSON string for the given [companyId].
  Future<Result<String>> getRecoverDataModels(String companyId);

  /// Returns all document templates for the given [companyId].
  Future<Result<List<DocumentTemplate>>> getDocumentTemplates(String companyId);

  /// Returns the document template identified by [templateId] within [companyId].
  Future<Result<DocumentTemplate>> getDocumentTemplateById(
      String companyId, String templateId);

  /// Creates a new document template and returns the generated id.
  Future<Result<String>> createDocumentTemplate(
    String companyId, {
    required String name,
    required String description,
    required int? validityInDays,
    required int? workload,
    required bool usePreviousPeriod,
    required bool acceptsSignature,
    String bodyFileName = '',
    String headerFileName = '',
    String footerFileName = '',
    String documentGroupId = '',
    List<int> recoverDataTypeIds = const [],
    List<PlaceSignatureData> placeSignatures = const [],
  });

  /// Updates an existing document template identified by [id] and returns its id.
  Future<Result<String>> updateDocumentTemplate(
    String companyId, {
    required String id,
    required String name,
    required String description,
    required int? validityInDays,
    required int? workload,
    required bool usePreviousPeriod,
    required bool acceptsSignature,
    String bodyFileName = '',
    String headerFileName = '',
    String footerFileName = '',
    String documentGroupId = '',
    List<int> recoverDataTypeIds = const [],
    List<PlaceSignatureData> placeSignatures = const [],
  });

  /// Returns whether the template has an uploaded file.
  Future<Result<bool>> hasFile(String companyId, String templateId);

  /// Uploads a file to the template.
  Future<Result<void>> uploadFile(
    String companyId,
    String templateId,
    Uint8List fileBytes,
    String fileName,
  );

  /// Downloads the file for the template. Returns raw bytes.
  Future<Result<Uint8List>> downloadFile(String companyId, String templateId);

  /// Returns the available type signature options.
  Future<Result<List<SelectionOption>>> getTypeSignatures(String companyId);
}
