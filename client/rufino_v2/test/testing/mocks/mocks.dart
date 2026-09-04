import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/data/services/auth_api_service.dart';
import 'package:rufino_v2/data/services/auth_code_api_service.dart';
import 'package:rufino_v2/data/services/oauth_login_strategy.dart';
import 'package:rufino_v2/domain/repositories/auth_repository.dart';

class MockAuthApiService extends Mock implements AuthApiService {}
class MockAuthCodeApiService extends Mock implements AuthCodeApiService {}
class MockOAuthLoginStrategy extends Mock implements OAuthLoginStrategy {}
class MockAuthRepository extends Mock implements AuthRepository {}
