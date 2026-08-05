import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/employee_profile.dart';
import 'package:rufino_v2/ui/features/employee/viewmodel/employee_profile_viewmodel.dart';

import '../../../../testing/fakes/fake_cep_repository.dart';
import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_department_repository.dart';
import '../../../../testing/fakes/fake_document_content_repository.dart';
import '../../../../testing/fakes/fake_document_group_repository.dart';
import '../../../../testing/fakes/fake_employee_repository.dart';
import '../../../../testing/fakes/fake_workplace_repository.dart';

/// The employee profile both reports an outdated snapshot AND offers to
/// rewrite it, so these tests cover the check, the refresh, and the guards
/// that keep an untrustworthy answer from reaching the user.
void main() {
  const company = Company(
    id: 'company-1',
    corporateName: 'Acme Corp',
    fantasyName: 'Acme',
    cnpj: '00000000000000',
  );

  const profile = EmployeeProfile(
    id: 'emp-1',
    name: 'Ana Lima',
    registration: 'R001',
    status: EmployeeStatus.active,
    roleId: 'role-1',
    workplaceId: 'wp-1',
  );

  late FakeCompanyRepository companyRepository;
  late FakeEmployeeRepository employeeRepository;
  late FakeDepartmentRepository departmentRepository;
  late FakeWorkplaceRepository workplaceRepository;
  late FakeDocumentGroupRepository documentGroupRepository;
  late FakeCepRepository cepRepository;
  late FakeDocumentContentRepository contentRepository;
  late EmployeeProfileViewModel viewModel;

  SelectedDocumentUnit selected(String id) => SelectedDocumentUnit(
        documentId: 'doc-$id',
        documentUnitId: id,
        documentName: 'Documento $id',
        documentUnitDate: '15/03/2026',
        canGenerate: true,
        hasFile: false,
      );

  setUp(() async {
    companyRepository = FakeCompanyRepository()..setSelectedCompany(company);
    employeeRepository = FakeEmployeeRepository()..setEmployeeProfile(profile);
    departmentRepository = FakeDepartmentRepository();
    workplaceRepository = FakeWorkplaceRepository();
    documentGroupRepository = FakeDocumentGroupRepository();
    cepRepository = FakeCepRepository();
    contentRepository = FakeDocumentContentRepository();

    viewModel = EmployeeProfileViewModel(
      companyRepository: companyRepository,
      employeeRepository: employeeRepository,
      departmentRepository: departmentRepository,
      workplaceRepository: workplaceRepository,
      documentGroupRepository: documentGroupRepository,
      cepRepository: cepRepository,
      documentContentRepository: contentRepository,
    );

    await viewModel.load('emp-1');
  });

  tearDown(() => viewModel.dispose());

  group('EmployeeProfileViewModel.checkOutdatedDocumentContent', () {
    test('returns only the units whose snapshot diverges', () async {
      contentRepository.markOutdated('u2');

      final outdated = await viewModel
          .checkOutdatedDocumentContent([selected('u1'), selected('u2')]);

      expect(outdated, {'u2'});
    });

    test('sends the profile employee id with every unit reference', () async {
      await viewModel.checkOutdatedDocumentContent([selected('u1')]);

      expect(contentRepository.lastCheckedItems.single.employeeId, 'emp-1');
      expect(contentRepository.lastCheckedItems.single.documentId, 'doc-u1');
    });

    test('does not warn when the check could not be concluded', () async {
      contentRepository.markCheckFailed('u1');

      expect(
        await viewModel.checkOutdatedDocumentContent([selected('u1')]),
        isEmpty,
      );
    });

    test('does not warn when the check itself fails', () async {
      contentRepository
        ..markOutdated('u1')
        ..setCheckShouldFail(true);

      expect(
        await viewModel.checkOutdatedDocumentContent([selected('u1')]),
        isEmpty,
      );
    });

    test('skips the call entirely for an empty selection', () async {
      expect(await viewModel.checkOutdatedDocumentContent([]), isEmpty);
      expect(contentRepository.checkCallCount, 0);
    });
  });

  group('EmployeeProfileViewModel.refreshDocumentContent', () {
    test('rewrites the snapshot and reports success to the user', () async {
      contentRepository.markOutdated('u1');

      final refreshed =
          await viewModel.refreshDocumentContent([selected('u1')]);

      expect(refreshed, isTrue);
      expect(contentRepository.refreshCallCount, 1);
      expect(viewModel.snackMessage, 'Informações do documento atualizadas.');
      expect(
        await viewModel.checkOutdatedDocumentContent([selected('u1')]),
        isEmpty,
      );
    });

    test('reports failure without claiming the snapshot was rewritten',
        () async {
      contentRepository.setRefreshShouldFail(true);

      final refreshed =
          await viewModel.refreshDocumentContent([selected('u1')]);

      expect(refreshed, isFalse);
      expect(viewModel.snackMessage, isNotNull);
    });

    test('does nothing without a content repository', () async {
      final isolated = EmployeeProfileViewModel(
        companyRepository: companyRepository,
        employeeRepository: employeeRepository,
        departmentRepository: departmentRepository,
        workplaceRepository: workplaceRepository,
        documentGroupRepository: documentGroupRepository,
        cepRepository: cepRepository,
      );
      addTearDown(isolated.dispose);
      await isolated.load('emp-1');

      expect(await isolated.refreshDocumentContent([selected('u1')]), isFalse);
      expect(contentRepository.refreshCallCount, 0);
    });
  });
}
