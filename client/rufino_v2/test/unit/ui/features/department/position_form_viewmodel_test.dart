import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/position.dart';
import 'package:rufino_v2/ui/features/department/viewmodel/position_form_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_department_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakePosition = Position(
  id: 'pos-1',
  name: 'Analista',
  description: 'Analista financeiro',
  cbo: '123456',
  roles: [],
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDepartmentRepository departmentRepository;
  late PositionFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    departmentRepository = FakeDepartmentRepository();
    viewModel = PositionFormViewModel(
      companyRepository: companyRepository,
      departmentRepository: departmentRepository,
      departmentId: 'dept-1',
    );
  });

  tearDown(() => viewModel.dispose());

  group('PositionFormViewModel', () {
    test('initial state is new position with idle status', () {
      expect(viewModel.isNew, true);
      expect(viewModel.status, PositionFormStatus.idle);
    });

    test('loadPosition populates controllers from repository', () async {
      departmentRepository.setPosition(_fakePosition);

      await viewModel.loadPosition('pos-1');

      expect(viewModel.isNew, false);
      expect(viewModel.status, PositionFormStatus.idle);
      expect(viewModel.nameController.text, 'Analista');
      expect(viewModel.descriptionController.text, 'Analista financeiro');
      expect(viewModel.cboController.text, '123456');
    });

    test('loadPosition transitions to error when repository fails', () async {
      departmentRepository.setShouldFail(true);

      await viewModel.loadPosition('pos-1');

      expect(viewModel.status, PositionFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('loadPosition does nothing when positionId is empty', () async {
      await viewModel.loadPosition('');

      expect(viewModel.isNew, true);
      expect(viewModel.status, PositionFormStatus.idle);
    });

    test('save for new position transitions to saved and calls createPosition',
        () async {
      viewModel.nameController.text = 'Gerente';
      viewModel.descriptionController.text = 'Gerente de TI';
      viewModel.cboController.text = '654321';

      await viewModel.save();

      expect(viewModel.status, PositionFormStatus.saved);
      expect(departmentRepository.lastCreatedPositionName, 'Gerente');
    });

    test('save for existing position transitions to saved', () async {
      departmentRepository.setPosition(_fakePosition);
      await viewModel.loadPosition('pos-1');

      viewModel.nameController.text = 'Analista Sênior';

      await viewModel.save();

      expect(viewModel.status, PositionFormStatus.saved);
    });

    test('save transitions to error when repository fails', () async {
      departmentRepository.setShouldFail(true);
      viewModel.nameController.text = 'Gerente';
      viewModel.descriptionController.text = 'Desc';
      viewModel.cboController.text = '123456';

      await viewModel.save();

      expect(viewModel.status, PositionFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('save transitions through saving state before completing', () async {
      final statuses = <PositionFormStatus>[];
      viewModel.addListener(() => statuses.add(viewModel.status));
      viewModel.nameController.text = 'Gerente';
      viewModel.descriptionController.text = 'Desc';
      viewModel.cboController.text = '123456';

      await viewModel.save();

      expect(statuses, containsAllInOrder([
        PositionFormStatus.saving,
        PositionFormStatus.saved,
      ]));
    });
  });
}
