import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/domain/repositories/auth_repository.dart';
import 'package:rufino_v2/domain/repositories/company_repository.dart';

class MockAuthRepository extends Mock implements AuthRepository {}
class MockCompanyRepository extends Mock implements CompanyRepository {}
