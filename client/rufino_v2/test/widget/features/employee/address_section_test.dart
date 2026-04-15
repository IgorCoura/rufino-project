import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/employee_profile.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';
import 'package:rufino_v2/ui/features/employee/viewmodel/employee_profile_viewmodel.dart';
import 'package:rufino_v2/ui/features/employee/widgets/components/address_section.dart';

import '../../../testing/fakes/fake_cep_repository.dart';
import '../../../testing/fakes/fake_company_repository.dart';
import '../../../testing/fakes/fake_department_repository.dart';
import '../../../testing/fakes/fake_document_group_repository.dart';
import '../../../testing/fakes/fake_employee_repository.dart';
import '../../../testing/fakes/fake_permission_repository.dart';
import '../../../testing/fakes/fake_workplace_repository.dart';

const _fakeProfile = EmployeeProfile(
  id: 'emp-1',
  name: 'Ana Lima',
  registration: 'R001',
  status: EmployeeStatus.active,
  roleId: 'role-1',
  workplaceId: 'wp-1',
);

const _emptyAddress = Address(
  zipCode: '',
  street: '',
  number: '',
  complement: '',
  neighborhood: '',
  city: '',
  state: '',
  country: '',
);

const _resolvedAddress = Address(
  zipCode: '01310100',
  street: 'Avenida Paulista',
  number: '',
  complement: '',
  neighborhood: 'Bela Vista',
  city: 'São Paulo',
  state: 'SP',
  country: 'Brasil',
);

Future<void> _pumpAddressSection(
  WidgetTester tester, {
  required EmployeeProfileViewModel viewModel,
  required PermissionNotifier permissionNotifier,
}) async {
  await tester.pumpWidget(
    MaterialApp(
      home: ChangeNotifierProvider<PermissionNotifier>.value(
        value: permissionNotifier,
        child: Scaffold(body: AddressSection(viewModel: viewModel)),
      ),
    ),
  );
}

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeEmployeeRepository employeeRepository;
  late FakeDepartmentRepository departmentRepository;
  late FakeWorkplaceRepository workplaceRepository;
  late FakeDocumentGroupRepository documentGroupRepository;
  late FakeCepRepository cepRepository;
  late PermissionNotifier permissionNotifier;
  late EmployeeProfileViewModel viewModel;

  setUp(() async {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(const Company(
        id: 'company-1',
        corporateName: 'Acme',
        fantasyName: 'Acme',
        cnpj: '00000000000000',
      ));
    employeeRepository = FakeEmployeeRepository()
      ..setEmployeeProfile(_fakeProfile)
      ..setAddress(_emptyAddress);
    departmentRepository = FakeDepartmentRepository();
    workplaceRepository = FakeWorkplaceRepository();
    documentGroupRepository = FakeDocumentGroupRepository();
    cepRepository = FakeCepRepository();

    viewModel = EmployeeProfileViewModel(
      companyRepository: companyRepository,
      employeeRepository: employeeRepository,
      departmentRepository: departmentRepository,
      workplaceRepository: workplaceRepository,
      documentGroupRepository: documentGroupRepository,
      cepRepository: cepRepository,
    );
    await viewModel.load('emp-1');
    await viewModel.loadAddress();

    final permRepo = FakePermissionRepository()
      ..setPermissions(const [
        Permission(resource: 'employee', scopes: ['view', 'edit']),
      ]);
    permissionNotifier = PermissionNotifier(permissionRepository: permRepo);
    await permissionNotifier.loadPermissions();
  });

  tearDown(() {
    viewModel.dispose();
    permissionNotifier.dispose();
  });

  group('AddressSection CEP autocomplete', () {
    testWidgets(
      'fills the other address fields when a complete CEP is entered',
      (tester) async {
        cepRepository.setAddress(_resolvedAddress);
        await _pumpAddressSection(
          tester,
          viewModel: viewModel,
          permissionNotifier: permissionNotifier,
        );

        await tester.tap(find.text('Editar'));
        await tester.pumpAndSettle();

        await tester.enterText(find.byType(TextFormField).first, '01310100');
        await tester.pumpAndSettle();

        expect(cepRepository.lookedUpCeps, ['01310100']);
        expect(find.widgetWithText(TextFormField, 'Avenida Paulista'),
            findsAtLeastNWidgets(1));
        expect(find.widgetWithText(TextFormField, 'Bela Vista'),
            findsAtLeastNWidgets(1));
        expect(find.widgetWithText(TextFormField, 'São Paulo'),
            findsAtLeastNWidgets(1));
        expect(find.widgetWithText(TextFormField, 'SP'),
            findsAtLeastNWidgets(1));
      },
    );

    testWidgets(
      'disables address fields while the CEP lookup is in flight',
      (tester) async {
        cepRepository
          ..setAddress(_resolvedAddress)
          ..setDelay(const Duration(milliseconds: 200));

        await _pumpAddressSection(
          tester,
          viewModel: viewModel,
          permissionNotifier: permissionNotifier,
        );

        await tester.tap(find.text('Editar'));
        await tester.pumpAndSettle();

        await tester.enterText(find.byType(TextFormField).first, '01310100');
        await tester.pump(); // triggers the listener but not the lookup future

        final streetField = tester.widget<TextFormField>(
          find.widgetWithText(TextFormField, 'Logradouro *'),
        );
        expect(streetField.enabled, isFalse);
        expect(find.byType(CircularProgressIndicator), findsOneWidget);

        await tester.pumpAndSettle();
        expect(find.byType(CircularProgressIndicator), findsNothing);
      },
    );

    testWidgets(
      'leaves address fields empty and shows no error when the lookup fails',
      (tester) async {
        cepRepository.setError('network down');
        await _pumpAddressSection(
          tester,
          viewModel: viewModel,
          permissionNotifier: permissionNotifier,
        );

        await tester.tap(find.text('Editar'));
        await tester.pumpAndSettle();

        await tester.enterText(find.byType(TextFormField).first, '01310100');
        await tester.pumpAndSettle();

        // No error text surfaced, no SnackBar.
        expect(find.byType(SnackBar), findsNothing);
        expect(find.text('CEP não encontrado'), findsNothing);
        // The other fields stay empty.
        expect(find.widgetWithText(TextFormField, 'Avenida Paulista'),
            findsNothing);
      },
    );
  });
}
