import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/domain/entities/document_group.dart';
import 'package:rufino_v2/domain/repositories/document_group_repository.dart';

/// In-memory fake implementation of [DocumentGroupRepository] for tests.
///
/// All responses are configurable via setters before each test.
class FakeDocumentGroupRepository implements DocumentGroupRepository {
  List<DocumentGroup> _groups = [];
  bool _shouldFail = false;

  void setGroups(List<DocumentGroup> groups) => _groups = groups;
  void setShouldFail(bool value) => _shouldFail = value;

  /// The name of the last group passed to [createDocumentGroup].
  String? lastCreatedGroupName;

  /// The id of the last group passed to [updateDocumentGroup].
  String? lastUpdatedGroupId;

  @override
  Future<Result<List<DocumentGroup>>> getDocumentGroups(
      String companyId) async {
    if (_shouldFail) {
      return Result.error(Exception('getDocumentGroups failed'));
    }
    return Result.success(_groups);
  }

  @override
  Future<Result<String>> createDocumentGroup(
    String companyId, {
    required String name,
    required String description,
  }) async {
    if (_shouldFail) {
      return Result.error(Exception('createDocumentGroup failed'));
    }
    lastCreatedGroupName = name;
    return const Result.success('new-group-id');
  }

  @override
  Future<Result<String>> updateDocumentGroup(
    String companyId, {
    required String id,
    required String name,
    required String description,
  }) async {
    if (_shouldFail) {
      return Result.error(Exception('updateDocumentGroup failed'));
    }
    lastUpdatedGroupId = id;
    return Result.success(id);
  }
}
