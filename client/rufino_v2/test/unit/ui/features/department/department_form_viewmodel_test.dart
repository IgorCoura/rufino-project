import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/department.dart';
import 'package:rufino_v2/ui/features/department/viewmodel/department_form_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_department_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeDepartment = Department(
  id: 'dept-1',
  name: 'Financeiro',
  description: 'Departamento financeiro',
  positions: [],
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeDepartmentRepository departmentRepository;
  late DepartmentFormViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    departmentRepository = FakeDepartmentRepository();
    viewModel = DepartmentFormViewModel(
      companyRepository: companyRepository,
      departmentRepository: departmentRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('DepartmentFormViewModel', () {
    test('initial state is new department with idle status', () {
      expect(viewModel.isNew, true);
      expect(viewModel.status, DepartmentFormStatus.idle);
      expect(viewModel.isLoading, false);
      expect(viewModel.isSaving, false);
    });

    test('loadDepartment populates name and description controllers from repository',
        () async {
      departmentRepository.setDepartment(_fakeDepartment);

      await viewModel.loadDepartment('dept-1');

      expect(viewModel.isNew, false);
      expect(viewModel.status, DepartmentFormStatus.idle);
      expect(viewModel.nameController.text, 'Financeiro');
      expect(viewModel.descriptionController.text, 'Departamento financeiro');
    });

    test('loadDepartment transitions to error when repository fails', () async {
      departmentRepository.setShouldFail(true);

      await viewModel.loadDepartment('dept-1');

      expect(viewModel.status, DepartmentFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('loadDepartment does nothing when departmentId is empty', () async {
      await viewModel.loadDepartment('');

      expect(viewModel.isNew, true);
      expect(viewModel.status, DepartmentFormStatus.idle);
    });

    test('save for new department transitions to saved and calls createDepartment',
        () async {
      viewModel.nameController.text = 'RH';
      viewModel.descriptionController.text = 'Recursos Humanos';

      await viewModel.save();

      expect(viewModel.status, DepartmentFormStatus.saved);
      expect(departmentRepository.lastCreatedDepartmentName, 'RH');
    });

    test('save for existing department transitions to saved and calls updateDepartment',
        () async {
      departmentRepository.setDepartment(_fakeDepartment);
      await viewModel.loadDepartment('dept-1');

      viewModel.nameController.text = 'Financeiro Atualizado';

      await viewModel.save();

      expect(viewModel.status, DepartmentFormStatus.saved);
      expect(departmentRepository.lastUpdatedDepartmentId, 'dept-1');
    });

    test('save transitions to error when repository fails', () async {
      departmentRepository.setShouldFail(true);
      viewModel.nameController.text = 'RH';
      viewModel.descriptionController.text = 'Recursos Humanos';

      await viewModel.save();

      expect(viewModel.status, DepartmentFormStatus.error);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('save transitions through saving state before completing', () async {
      final statuses = <DepartmentFormStatus>[];
      viewModel.addListener(() => statuses.add(viewModel.status));
      viewModel.nameController.text = 'RH';
      viewModel.descriptionController.text = 'Desc';

      await viewModel.save();

      expect(statuses, containsAllInOrder([
        DepartmentFormStatus.saving,
        DepartmentFormStatus.saved,
      ]));
    });
  });
}
