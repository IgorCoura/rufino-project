import 'dart:typed_data';

import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/document_template.dart';
import 'package:rufino_v2/domain/entities/selection_option.dart';
import 'package:rufino_v2/domain/repositories/document_template_repository.dart';

/// In-memory fake implementation of [DocumentTemplateRepository] for tests.
///
/// All responses are configurable via setters before each test.
class FakeDocumentTemplateRepository implements DocumentTemplateRepository {
  List<DocumentTemplate> _templates = [];
  DocumentTemplate? _template;
  bool _shouldFail = false;

  void setTemplates(List<DocumentTemplate> templates) =>
      _templates = templates;
  void setTemplate(DocumentTemplate? t) => _template = t;
  void setShouldFail(bool value) => _shouldFail = value;

  String? lastCreatedTemplateName;
  String? lastUpdatedTemplateId;

  @override
  Future<Result<List<DocumentTemplate>>> getDocumentTemplates(
      String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getDocumentTemplates failed'));
    }
    return Result.success(_templates);
  }

  @override
  Future<Result<DocumentTemplate>> getDocumentTemplateById(
      String companyId, String templateId) async {
    if (_shouldFail) {
      return Result.error(Exception('getDocumentTemplateById failed'));
    }
    if (_template == null) {
      return Result.error(Exception('DocumentTemplate not found'));
    }
    return Result.success(_template!);
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
    if (_shouldFail) {
      return Result.error(Exception('createDocumentTemplate failed'));
    }
    lastCreatedTemplateName = name;
    return const Result.success('new-template-id');
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
    if (_shouldFail) {
      return Result.error(Exception('updateDocumentTemplate failed'));
    }
    lastUpdatedTemplateId = id;
    return Result.success(id);
  }

  @override
  Future<Result<List<SelectionOption>>> getDocumentGroups(
      String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getDocumentGroups failed'));
    }
    return const Result.success([
      SelectionOption(id: 'grp-1', name: 'Admissão'),
      SelectionOption(id: 'grp-2', name: 'Periódicos'),
    ]);
  }

  @override
  Future<Result<List<SelectionOption>>> getRecoverDataTypes(
      String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getRecoverDataTypes failed'));
    }
    return const Result.success([
      SelectionOption(id: '1', name: 'Dados da Empresa'),
      SelectionOption(id: '3', name: 'Dados do Funcionário'),
    ]);
  }

  @override
  Future<Result<String>> getRecoverDataModels(String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getRecoverDataModels failed'));
    }
    return const Result.success('{"company":{"name":"string"}}');
  }

  @override
  Future<Result<bool>> hasFile(String companyId, String templateId) async {
    if (_shouldFail) return Result.error(Exception('hasFile failed'));
    return const Result.success(false);
  }

  @override
  Future<Result<void>> uploadFile(
    String companyId,
    String templateId,
    Uint8List fileBytes,
    String fileName,
  ) async {
    if (_shouldFail) return Result.error(Exception('uploadFile failed'));
    return const Result<void>.success(null);
  }

  @override
  Future<Result<Uint8List>> downloadFile(
      String companyId, String templateId) async {
    if (_shouldFail) return Result.error(Exception('downloadFile failed'));
    return Result.success(Uint8List.fromList([0]));
  }

  @override
  Future<Result<List<SelectionOption>>> getTypeSignatures(
      String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getTypeSignatures failed'));
    }
    return const Result.success([
      SelectionOption(id: '1', name: 'Assinatura'),
      SelectionOption(id: '2', name: 'Visto'),
    ]);
  }
}
