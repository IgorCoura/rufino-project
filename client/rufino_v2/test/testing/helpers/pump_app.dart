import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/domain/repositories/auth_repository.dart';
import 'package:rufino_v2/domain/repositories/company_repository.dart';

import '../fakes/fake_auth_repository.dart';
import '../fakes/fake_company_repository.dart';

extension PumpApp on WidgetTester {
  Future<void> pumpApp(
    Widget widget, {
    AuthRepository? authRepository,
    CompanyRepository? companyRepository,
  }) async {
    await pumpWidget(
      MultiProvider(
        providers: [
          Provider<AuthRepository>.value(
            value: authRepository ?? FakeAuthRepository(),
          ),
          Provider<CompanyRepository>.value(
            value: companyRepository ?? FakeCompanyRepository(),
          ),
        ],
        child: MaterialApp(home: widget),
      ),
    );
  }
}
