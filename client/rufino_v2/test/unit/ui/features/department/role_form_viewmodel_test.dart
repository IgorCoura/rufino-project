import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/remuneration.dart';
import 'package:rufino_v2/domain/entities/role.dart';
import 'package:rufino_v2/ui/features/department/viewmodel/role_form_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_department_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakePaymentUnits = [
  PaymentUnit(id: '5', name: 'Por Mês'),
  PaymentUnit(id: '1', name: 'Por Hora'),
];

const _fakeSalaryTypes = [
  SalaryType(id: '1', name: 'BRL'),
  SalaryType(id: '2', name: 'USD'),
];

const _fakeRole = Role(
  id: 'role-1',
  name: 'Analista Jr',
  description: 'Analista júnior',
  cbo: '123456',
  remuneration: Remuneration(
    paymentUnit: PaymentUnit(id: '5', name: 'Por Mês'),
    baseSalary: BaseSalary(type: SalaryType(id: '1', name: 'BRL'), value: '3500.00'),
    description: 'Salário mensal',
  ),
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDepartmentRepository departmentRepository;
  late RoleFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    departmentRepository = FakeDepartmentRepository()
      ..setPaymentUnits(_fakePaymentUnits)
      ..setSalaryTypes(_fakeSalaryTypes);
    viewModel = RoleFormViewModel(
      companyRepository: companyRepository,
      departmentRepository: departmentRepository,
      positionId: 'pos-1',
    );
  });

  tearDown(() => viewModel.dispose());

  group('RoleFormViewModel', () {
    test('initial state is new role with idle status', () {
      expect(viewModel.isNew, true);
      expect(viewModel.status, RoleFormStatus.idle);
    });

    test('initialize loads payment units and salary types', () async {
      await viewModel.initialize();

      expect(viewModel.status, RoleFormStatus.idle);
      expect(viewModel.paymentUnits, hasLength(2));
      expect(viewModel.salaryTypes, hasLength(2));
      expect(viewModel.paymentUnits.first.name, 'Por Mês');
    });

    test('initialize with roleId also loads existing role data', () async {
      departmentRepository.setRole(_fakeRole);

      await viewModel.initialize(roleId: 'role-1');

      expect(viewModel.isNew, false);
      expect(viewModel.nameController.text, 'Analista Jr');
      expect(viewModel.cboController.text, '123456');
      expect(viewModel.paymentUnitId, '5');
      expect(viewModel.salaryTypeId, '1');
      expect(viewModel.salaryValueController.text, '3500.00');
    });

    test('initialize transitions to error when lookup data fetch fails',
        () async {
      departmentRepository.setShouldFail(true);

      await viewModel.initialize();

      expect(viewModel.status, RoleFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('save for new role transitions to saved and calls createRole', () async {
      await viewModel.initialize();
      viewModel.nameController.text = 'Dev Jr';
      viewModel.descriptionController.text = 'Desenvolvedor júnior';
      viewModel.cboController.text = '317110';
      viewModel.setPaymentUnitId('5');
      viewModel.setSalaryTypeId('1');
      viewModel.salaryValueController.text = '4000.00';
      viewModel.remunerationDescriptionController.text = 'Salário fixo';

      await viewModel.save();

      expect(viewModel.status, RoleFormStatus.saved);
      expect(departmentRepository.lastCreatedRoleName, 'Dev Jr');
    });

    test('save for existing role transitions to saved', () async {
      departmentRepository.setRole(_fakeRole);
      await viewModel.initialize(roleId: 'role-1');

      viewModel.nameController.text = 'Analista Pleno';

      await viewModel.save();

      expect(viewModel.status, RoleFormStatus.saved);
    });

    test('save transitions to error when repository fails', () async {
      await viewModel.initialize();

      departmentRepository.setShouldFail(true);

      viewModel.nameController.text = 'Dev';
      viewModel.descriptionController.text = 'Desc';
      viewModel.cboController.text = '123456';
      viewModel.setPaymentUnitId('5');
      viewModel.setSalaryTypeId('1');
      viewModel.salaryValueController.text = '3000.00';
      viewModel.remunerationDescriptionController.text = 'Desc remuneração';

      await viewModel.save();

      expect(viewModel.status, RoleFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('setPaymentUnitId and setSalaryTypeId notify listeners', () async {
      await viewModel.initialize();
      int notifyCount = 0;
      viewModel.addListener(() => notifyCount++);

      viewModel.setPaymentUnitId('1');
      viewModel.setSalaryTypeId('2');

      expect(notifyCount, 2);
      expect(viewModel.paymentUnitId, '1');
      expect(viewModel.salaryTypeId, '2');
    });

    test('save transitions through saving state before completing', () async {
      await viewModel.initialize();
      final statuses = <RoleFormStatus>[];
      viewModel.addListener(() => statuses.add(viewModel.status));

      viewModel.nameController.text = 'Dev';
      viewModel.descriptionController.text = 'Desc';
      viewModel.cboController.text = '123456';
      viewModel.setPaymentUnitId('5');
      viewModel.setSalaryTypeId('1');
      viewModel.salaryValueController.text = '3000.00';
      viewModel.remunerationDescriptionController.text = 'Desc';

      await viewModel.save();

      expect(statuses, containsAllInOrder([
        RoleFormStatus.saving,
        RoleFormStatus.saved,
      ]));
    });
  });
}
