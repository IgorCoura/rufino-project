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

/// Scheduling the signature send is the profile's alternative to sending it
/// now, so these tests cover what the view model forwards to the repository and
/// what it reports back to the user.
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

  late FakeEmployeeRepository employeeRepository;
  late EmployeeProfileViewModel viewModel;

  setUp(() async {
    employeeRepository = FakeEmployeeRepository()..setEmployeeProfile(profile);

    viewModel = EmployeeProfileViewModel(
      companyRepository: FakeCompanyRepository()..setSelectedCompany(company),
      employeeRepository: employeeRepository,
      departmentRepository: FakeDepartmentRepository(),
      workplaceRepository: FakeWorkplaceRepository(),
      documentGroupRepository: FakeDocumentGroupRepository(),
      cepRepository: FakeCepRepository(),
      documentContentRepository: FakeDocumentContentRepository(),
    );

    await viewModel.load('emp-1');
  });

  tearDown(() => viewModel.dispose());

  group('EmployeeProfileViewModel.scheduleSendToSign', () {
    test('forwards both dates to the repository', () async {
      await viewModel.scheduleSendToSign(
          'doc-1', 'unit-1', '30/06/2026', '05/07/2026', 0);

      expect(employeeRepository.lastScheduledSend?.sendOn, '30/06/2026');
      expect(
          employeeRepository.lastScheduledSend?.dateLimitToSign, '05/07/2026');
    });

    test('reports the scheduled date back to the user', () async {
      await viewModel.scheduleSendToSign(
          'doc-1', 'unit-1', '30/06/2026', '05/07/2026', 0);

      expect(viewModel.snackMessage,
          'Envio para assinatura agendado para 30/06/2026.');
    });

    test('reports a failure message when the repository fails', () async {
      employeeRepository.setShouldFail(true);

      await viewModel.scheduleSendToSign(
          'doc-1', 'unit-1', '30/06/2026', '05/07/2026', 0);

      expect(viewModel.snackMessage, isNotNull);
      expect(employeeRepository.lastScheduledSend, isNull);
    });
  });

  group('EmployeeProfileViewModel.cancelScheduledSendToSign', () {
    test('asks the repository to drop the schedule', () async {
      await viewModel.cancelScheduledSendToSign('doc-1', 'unit-1');

      expect(employeeRepository.cancelScheduledSendCalled, isTrue);
      expect(viewModel.snackMessage, 'Agendamento cancelado.');
    });

    test('reports a failure message when the repository fails', () async {
      employeeRepository.setShouldFail(true);

      await viewModel.cancelScheduledSendToSign('doc-1', 'unit-1');

      expect(viewModel.snackMessage, isNotNull);
      expect(employeeRepository.cancelScheduledSendCalled, isFalse);
    });
  });
}
