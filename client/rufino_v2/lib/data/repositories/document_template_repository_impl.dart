import 'dart:typed_data';

import '../../core/errors/document_template_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/document_template.dart';
import '../../domain/entities/selection_option.dart';
import '../../domain/repositories/document_template_repository.dart';
import '../models/document_template_api_model.dart';
import '../services/document_template_api_service.dart';

/// Concrete implementation of [DocumentTemplateRepository] backed by
/// [DocumentTemplateApiService].
///
/// All service calls are wrapped in try/catch. [DocumentTemplateException]
/// subtypes are propagated as-is; all other errors are wrapped in
/// [DocumentTemplateNetworkException].
class DocumentTemplateRepositoryImpl implements DocumentTemplateRepository {
  const DocumentTemplateRepositoryImpl({required this.apiService});

  final DocumentTemplateApiService apiService;

  @override
  Future<Result<List<DocumentTemplate>>> getDocumentTemplates(
      String companyId) async {
    try {
      final models = await apiService.getDocumentTemplates(companyId);
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }

  @override
  Future<Result<DocumentTemplate>> getDocumentTemplateById(
      String companyId, String templateId) async {
    try {
      final model =
          await apiService.getDocumentTemplateById(companyId, templateId);
      return Result.success(model.toEntity());
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }

  @override
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
  }) async {
    try {
      final model = DocumentTemplateApiModel(
        id: '',
        name: name,
        description: description,
        validityDurationInDays: (validityInDays ?? 0).toDouble(),
        workloadInHours: (workload ?? 0).toDouble(),
        usePreviousPeriod: usePreviousPeriod,
        acceptsSignature: acceptsSignature,
        bodyFileName: bodyFileName,
        headerFileName: headerFileName,
        footerFileName: footerFileName,
        documentGroupId: documentGroupId,
        recoverDataTypeIds: recoverDataTypeIds,
        placeSignatures: placeSignatures,
      );
      final id = await apiService.createDocumentTemplate(companyId, model);
      return Result.success(id);
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }

  @override
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
  }) async {
    try {
      final model = DocumentTemplateApiModel(
        id: id,
        name: name,
        description: description,
        validityDurationInDays: (validityInDays ?? 0).toDouble(),
        workloadInHours: (workload ?? 0).toDouble(),
        usePreviousPeriod: usePreviousPeriod,
        acceptsSignature: acceptsSignature,
        bodyFileName: bodyFileName,
        headerFileName: headerFileName,
        footerFileName: footerFileName,
        documentGroupId: documentGroupId,
        recoverDataTypeIds: recoverDataTypeIds,
        placeSignatures: placeSignatures,
      );
      final returnedId =
          await apiService.updateDocumentTemplate(companyId, model);
      return Result.success(returnedId);
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }

  @override
  Future<Result<List<SelectionOption>>> getDocumentGroups(
      String companyId) async {
    try {
      final list = await apiService.getDocumentGroups(companyId);
      return Result.success(list
          .map((j) => SelectionOption(
                id: j['id'] as String? ?? '',
                name: j['name'] as String? ?? '',
              ))
          .toList());
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }

  @override
  Future<Result<List<SelectionOption>>> getRecoverDataTypes(
      String companyId) async {
    try {
      final list = await apiService.getRecoverDataTypes(companyId);
      return Result.success(list
          .map((j) => SelectionOption(
                id: (j['id']).toString(),
                name: j['name'] as String? ?? '',
              ))
          .toList());
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }

  @override
  Future<Result<String>> getRecoverDataModels(String companyId) async {
    try {
      final json = await apiService.getRecoverDataModels(companyId);
      return Result.success(json);
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }

  @override
  Future<Result<bool>> hasFile(String companyId, String templateId) async {
    try {
      final result = await apiService.hasFile(companyId, templateId);
      return Result.success(result);
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }

  @override
  Future<Result<void>> uploadFile(
    String companyId,
    String templateId,
    Uint8List fileBytes,
    String fileName,
  ) async {
    try {
      await apiService.uploadFile(companyId, templateId, fileBytes, fileName);
      return const Result<void>.success(null);
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }

  @override
  Future<Result<Uint8List>> downloadFile(
      String companyId, String templateId) async {
    try {
      final bytes = await apiService.downloadFile(companyId, templateId);
      return Result.success(bytes);
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }

  @override
  Future<Result<List<SelectionOption>>> getTypeSignatures(
      String companyId) async {
    try {
      final list = await apiService.getTypeSignatures(companyId);
      return Result.success(list
          .map((j) => SelectionOption(
                id: (j['id']).toString(),
                name: j['name'] as String? ?? '',
              ))
          .toList());
    } on DocumentTemplateException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(DocumentTemplateNetworkException(e));
    }
  }
}
