import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/domain/repositories/auth_repository.dart';
import 'package:rufino_v2/domain/repositories/batch_document_repository.dart';
import 'package:rufino_v2/domain/repositories/company_repository.dart';
import 'package:rufino_v2/domain/repositories/department_repository.dart';
import 'package:rufino_v2/domain/repositories/document_group_repository.dart';
import 'package:rufino_v2/domain/repositories/document_template_repository.dart';
import 'package:rufino_v2/domain/repositories/workplace_repository.dart';

class MockAuthRepository extends Mock implements AuthRepository {}
class MockBatchDocumentRepository extends Mock implements BatchDocumentRepository {}
class MockCompanyRepository extends Mock implements CompanyRepository {}
class MockDepartmentRepository extends Mock implements DepartmentRepository {}
class MockDocumentGroupRepository extends Mock implements DocumentGroupRepository {}
class MockDocumentTemplateRepository extends Mock implements DocumentTemplateRepository {}
class MockWorkplaceRepository extends Mock implements WorkplaceRepository {}
