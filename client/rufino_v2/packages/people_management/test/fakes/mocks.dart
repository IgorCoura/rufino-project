import 'package:mocktail/mocktail.dart';
import 'package:people_management/people_management.dart';

/// Mocks dos repositórios deste produto.
///
/// Cada pacote tem os seus, como o `bill_payment` — fake de teste não atravessa
/// a fronteira entre produtos, porque `test/` não é exportado.
class MockBatchDocumentRepository extends Mock implements BatchDocumentRepository {}

class MockCompanyRepository extends Mock implements CompanyRepository {}

class MockDepartmentRepository extends Mock implements DepartmentRepository {}

class MockDocumentGroupRepository extends Mock implements DocumentGroupRepository {}

class MockDocumentTemplateRepository extends Mock implements DocumentTemplateRepository {}

class MockDocumentScannerService extends Mock implements DocumentScannerService {}

class MockDocumentScannerRepository extends Mock implements DocumentScannerRepository {}

class MockWorkplaceRepository extends Mock implements WorkplaceRepository {}
