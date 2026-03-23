import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/company.dart';
import 'package:rufino_v2/domain/entities/employee.dart';
import 'package:rufino_v2/domain/entities/employee_contact.dart';
import 'package:rufino_v2/domain/entities/employee_id_card.dart';
import 'package:rufino_v2/domain/entities/employee_personal_info.dart';
import 'package:rufino_v2/domain/entities/employee_profile.dart';
import 'package:rufino_v2/domain/entities/department.dart';
import 'package:rufino_v2/domain/entities/employee_medical_exam.dart';
import 'package:rufino_v2/domain/entities/employee_military_document.dart';
import 'package:rufino_v2/domain/entities/position.dart';
import 'package:rufino_v2/domain/entities/employee_vote_id.dart';
import 'package:rufino_v2/domain/entities/remuneration.dart';
import 'package:rufino_v2/domain/entities/role.dart';
import 'package:rufino_v2/domain/entities/workplace.dart';
import 'package:rufino_v2/ui/features/employee/viewmodel/employee_profile_viewmodel.dart';

import '../../../../testing/fakes/fake_company_repository.dart';
import '../../../../testing/fakes/fake_department_repository.dart';
import '../../../../testing/fakes/fake_employee_repository.dart';
import '../../../../testing/fakes/fake_workplace_repository.dart';

const _fakeCompany = Company(
  id: 'company-1',
  corporateName: 'Acme Corp',
  fantasyName: 'Acme',
  cnpj: '00000000000000',
);

const _fakeProfile = EmployeeProfile(
  id: 'emp-1',
  name: 'Ana Lima',
  registration: 'R001',
  status: EmployeeStatus.active,
  roleId: 'role-1',
  workplaceId: 'wp-1',
);

const _fakeRole = Role(
  id: 'role-1',
  name: 'Analista',
  description: 'Analista financeira',
  cbo: '123456',
  remuneration: Remuneration(
    paymentUnit: PaymentUnit(id: '5', name: 'Por Mês'),
    baseSalary: BaseSalary(
      type: SalaryType(id: '1', name: 'BRL'),
      value: '3500.00',
    ),
    description: 'Salário mensal',
  ),
);

const _fakeWorkplace = Workplace(
  id: 'wp-1',
  name: 'Sede Principal',
  address: Address(
    zipCode: '01310100',
    street: 'Av. Paulista',
    number: '1000',
    complement: '',
    neighborhood: 'Bela Vista',
    city: 'São Paulo',
    state: 'SP',
    country: 'Brasil',
  ),
);

void main() {
  late FakeCompanyRepository companyRepository;
  late FakeEmployeeRepository employeeRepository;
  late FakeDepartmentRepository departmentRepository;
  late FakeWorkplaceRepository workplaceRepository;
  late EmployeeProfileViewModel viewModel;

  setUp(() {
    companyRepository = FakeCompanyRepository()
      ..setSelectedCompany(_fakeCompany);
    employeeRepository = FakeEmployeeRepository()
      ..setEmployeeProfile(_fakeProfile);
    departmentRepository = FakeDepartmentRepository()..setRole(_fakeRole);
    workplaceRepository = FakeWorkplaceRepository()
      ..setWorkplace(_fakeWorkplace);

    viewModel = EmployeeProfileViewModel(
      companyRepository: companyRepository,
      employeeRepository: employeeRepository,
      departmentRepository: departmentRepository,
      workplaceRepository: workplaceRepository,
    );
  });

  tearDown(() => viewModel.dispose());

  group('EmployeeProfileViewModel', () {
    test('starts idle with no loaded profile', () {
      expect(viewModel.status, EmployeeProfileStatus.idle);
      expect(viewModel.profile, isNull);
      expect(viewModel.imageBytes, isNull);
    });

    test('loads profile, role, workplace, and image successfully', () async {
      final imageBytes = Uint8List.fromList([1, 2, 3]);
      employeeRepository.setImageBytes(imageBytes);

      await viewModel.load('emp-1');

      expect(viewModel.status, EmployeeProfileStatus.idle);
      expect(viewModel.profile?.name, 'Ana Lima');
      expect(viewModel.roleName, 'Analista');
      expect(viewModel.workplaceName, 'Sede Principal');
      expect(viewModel.imageBytes, imageBytes);
      expect(employeeRepository.lastProfileEmployeeId, 'emp-1');
    });

    test('sets error when there is no selected company', () async {
      companyRepository.setSelectedCompany(null);

      await viewModel.load('emp-1');

      expect(viewModel.status, EmployeeProfileStatus.error);
      expect(viewModel.errorMessage, contains('empresa'));
    });

    test('sets error when the profile repository call fails', () async {
      employeeRepository.setShouldFail(true);

      await viewModel.load('emp-1');

      expect(viewModel.status, EmployeeProfileStatus.error);
      expect(viewModel.errorMessage, contains('funcionário'));
    });

    test('uses fallback labels when role and workplace are not assigned',
        () async {
      employeeRepository.setEmployeeProfile(
        const EmployeeProfile(
          id: 'emp-1',
          name: 'Ana Lima',
          registration: 'R001',
          status: EmployeeStatus.pending,
          roleId: '',
          workplaceId: '',
        ),
      );

      await viewModel.load('emp-1');

      expect(viewModel.roleLabel, 'Sem função atribuída');
      expect(viewModel.workplaceLabel, 'Sem local de trabalho atribuído');
    });

    test('markAsInactive updates the profile status and exposes a snack message',
        () async {
      await viewModel.load('emp-1');

      await viewModel.markAsInactive();

      expect(viewModel.status, EmployeeProfileStatus.idle);
      expect(viewModel.profile?.status, EmployeeStatus.inactive);
      expect(viewModel.snackMessage, contains('inativo'));
      expect(employeeRepository.lastMarkInactiveEmployeeId, 'emp-1');
    });

    test('markAsInactive sets error when the repository update fails',
        () async {
      await viewModel.load('emp-1');
      employeeRepository.setShouldFail(true);

      await viewModel.markAsInactive();

      expect(viewModel.status, EmployeeProfileStatus.error);
      expect(viewModel.errorMessage, contains('status'));
    });

    test('consumeSnackMessage clears the transient message', () async {
      await viewModel.load('emp-1');
      await viewModel.markAsInactive();

      viewModel.consumeSnackMessage();

      expect(viewModel.snackMessage, isNull);
    });

    group('name editing', () {
      test('startEditingName enters editing mode with the current name',
          () async {
        await viewModel.load('emp-1');

        viewModel.startEditingName();

        expect(viewModel.isEditingName, isTrue);
        expect(viewModel.pendingName, 'Ana Lima');
      });

      test('cancelEditingName exits editing mode without saving', () async {
        await viewModel.load('emp-1');
        viewModel.startEditingName();

        viewModel.cancelEditingName();

        expect(viewModel.isEditingName, isFalse);
        expect(viewModel.pendingName, isEmpty);
        expect(viewModel.profile?.name, 'Ana Lima');
      });

      test('saveName persists the new name and exits editing mode', () async {
        await viewModel.load('emp-1');
        viewModel.startEditingName();

        await viewModel.saveName('Ana Souza');

        expect(viewModel.isEditingName, isFalse);
        expect(viewModel.profile?.name, 'Ana Souza');
        expect(viewModel.snackMessage, contains('Nome'));
        expect(viewModel.status, EmployeeProfileStatus.idle);
        expect(employeeRepository.lastEditedName, 'Ana Souza');
      });

      test('saveName trims whitespace before persisting', () async {
        await viewModel.load('emp-1');
        viewModel.startEditingName();

        await viewModel.saveName('  Ana Souza  ');

        expect(viewModel.profile?.name, 'Ana Souza');
        expect(employeeRepository.lastEditedName, 'Ana Souza');
      });

      test('saveName does nothing when the trimmed name is empty', () async {
        await viewModel.load('emp-1');
        viewModel.startEditingName();

        await viewModel.saveName('   ');

        expect(viewModel.profile?.name, 'Ana Lima');
        expect(employeeRepository.lastEditedName, isNull);
      });

      test('saveName sets error when the repository call fails', () async {
        await viewModel.load('emp-1');
        viewModel.startEditingName();
        employeeRepository.setShouldFail(true);

        await viewModel.saveName('Ana Souza');

        expect(viewModel.status, EmployeeProfileStatus.error);
        expect(viewModel.errorMessage, contains('nome'));
      });
    });

    group('avatar upload', () {
      test('uploadAvatar updates imageBytes and exposes a snack message',
          () async {
        await viewModel.load('emp-1');
        final newBytes = Uint8List.fromList([10, 20, 30]);

        await viewModel.uploadAvatar(newBytes, 'photo.jpg');

        expect(viewModel.imageBytes, newBytes);
        expect(viewModel.snackMessage, contains('foto'));
        expect(viewModel.status, EmployeeProfileStatus.idle);
        expect(employeeRepository.lastUploadedFileName, 'photo.jpg');
      });

      test('uploadAvatar rejects images larger than 5 MB', () async {
        await viewModel.load('emp-1');
        final largeBytes = Uint8List(5 * 1024 * 1024 + 1);

        await viewModel.uploadAvatar(largeBytes, 'big.jpg');

        expect(viewModel.status, EmployeeProfileStatus.idle);
        expect(viewModel.snackMessage, contains('5 MB'));
        expect(employeeRepository.lastUploadedFileName, isNull);
      });

      test('uploadAvatar sets error when the repository call fails', () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);
        final bytes = Uint8List.fromList([1, 2, 3]);

        await viewModel.uploadAvatar(bytes, 'photo.png');

        expect(viewModel.status, EmployeeProfileStatus.error);
        expect(viewModel.errorMessage, contains('foto'));
      });
    });

    group('contact section', () {
      test('loadContact transitions from notLoaded to loaded with data',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setContact(
          const EmployeeContact(
              cellphone: '11999990000', email: 'user@test.com'),
        );

        await viewModel.loadContact();

        expect(viewModel.contactStatus, SectionLoadStatus.loaded);
        expect(viewModel.contact?.cellphone, '11999990000');
        expect(viewModel.contact?.email, 'user@test.com');
      });

      test('loadContact sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);

        await viewModel.loadContact();

        expect(viewModel.contactStatus, SectionLoadStatus.error);
        expect(viewModel.contact, isNull);
      });

      test('loadContact does nothing when already loaded', () async {
        await viewModel.load('emp-1');
        await viewModel.loadContact();
        final firstContact = viewModel.contact;

        // Change underlying data and call again — should not reload.
        employeeRepository.setContact(
          const EmployeeContact(cellphone: '999', email: 'other@test.com'),
        );
        await viewModel.loadContact();

        expect(viewModel.contact, firstContact);
      });

      test('saveContact persists new values and shows a snack message',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadContact();

        await viewModel.saveContact('11888880000', 'new@email.com');

        expect(viewModel.contactStatus, SectionLoadStatus.loaded);
        expect(viewModel.contact?.cellphone, '11888880000');
        expect(viewModel.contact?.email, 'new@email.com');
        expect(viewModel.snackMessage, contains('Contato'));
        expect(employeeRepository.lastSavedContactCellphone, '11888880000');
      });

      test('saveContact sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadContact();
        employeeRepository.setShouldFail(true);

        await viewModel.saveContact('11888880000', 'new@email.com');

        expect(viewModel.contactStatus, SectionLoadStatus.error);
      });
    });

    group('address section', () {
      const newAddress = Address(
        zipCode: '04538-132',
        street: 'Av. Brigadeiro Faria Lima',
        number: '3900',
        complement: 'Andar 10',
        neighborhood: 'Itaim Bibi',
        city: 'São Paulo',
        state: 'SP',
        country: 'Brasil',
      );

      test('loadAddress transitions from notLoaded to loaded with data',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setAddress(newAddress);

        await viewModel.loadAddress();

        expect(viewModel.addressStatus, SectionLoadStatus.loaded);
        expect(viewModel.address?.street, 'Av. Brigadeiro Faria Lima');
      });

      test('loadAddress sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);

        await viewModel.loadAddress();

        expect(viewModel.addressStatus, SectionLoadStatus.error);
      });

      test('saveAddress persists new values and shows a snack message',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadAddress();

        await viewModel.saveAddress(newAddress);

        expect(viewModel.addressStatus, SectionLoadStatus.loaded);
        expect(viewModel.address?.street, 'Av. Brigadeiro Faria Lima');
        expect(viewModel.snackMessage, contains('Endereço'));
        expect(employeeRepository.lastSavedAddress?.zipCode, '04538-132');
      });

      test('saveAddress sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadAddress();
        employeeRepository.setShouldFail(true);

        await viewModel.saveAddress(newAddress);

        expect(viewModel.addressStatus, SectionLoadStatus.error);
      });
    });

    group('personal info section', () {
      const newPersonalInfo = EmployeePersonalInfo(
        genderId: '1',
        maritalStatusId: '2',
        ethnicityId: '3',
        educationLevelId: '4',
        disabilityIds: ['10'],
        disabilityObservation: 'Visual',
      );

      test('loadPersonalInfo transitions to loaded with data', () async {
        await viewModel.load('emp-1');
        employeeRepository.setPersonalInfo(newPersonalInfo);

        await viewModel.loadPersonalInfo();

        expect(viewModel.personalInfoStatus, SectionLoadStatus.loaded);
        expect(viewModel.personalInfo?.genderId, '1');
      });

      test('loadPersonalInfo sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);

        await viewModel.loadPersonalInfo();

        expect(viewModel.personalInfoStatus, SectionLoadStatus.error);
      });

      test('savePersonalInfo persists new values and shows a snack message',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadPersonalInfo();

        await viewModel.savePersonalInfo(newPersonalInfo);

        expect(viewModel.personalInfoStatus, SectionLoadStatus.loaded);
        expect(viewModel.personalInfo?.ethnicityId, '3');
        expect(viewModel.snackMessage, contains('pessoais'));
        expect(
            employeeRepository.lastSavedPersonalInfo?.disabilityIds, ['10']);
      });

      test(
          'savePersonalInfo sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadPersonalInfo();
        employeeRepository.setShouldFail(true);

        await viewModel.savePersonalInfo(newPersonalInfo);

        expect(viewModel.personalInfoStatus, SectionLoadStatus.error);
      });
    });

    group('id card section', () {
      const newIdCard = EmployeeIdCard(
        cpf: '123.456.789-00',
        motherName: 'Maria',
        fatherName: 'João',
        dateOfBirth: '15/06/1990',
        birthCity: 'São Paulo',
        birthState: 'SP',
        nationality: 'Brasileira',
      );

      test('loadIdCard transitions from notLoaded to loaded with data',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setIdCard(newIdCard);

        await viewModel.loadIdCard();

        expect(viewModel.idCardStatus, SectionLoadStatus.loaded);
        expect(viewModel.idCard?.cpf, '123.456.789-00');
      });

      test('loadIdCard sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);

        await viewModel.loadIdCard();

        expect(viewModel.idCardStatus, SectionLoadStatus.error);
      });

      test('saveIdCard persists new values and shows a snack message',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadIdCard();

        await viewModel.saveIdCard(newIdCard);

        expect(viewModel.idCardStatus, SectionLoadStatus.loaded);
        expect(viewModel.idCard?.motherName, 'Maria');
        expect(viewModel.snackMessage, contains('documento'));
        expect(employeeRepository.lastSavedIdCard?.cpf, '123.456.789-00');
      });

      test('saveIdCard sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadIdCard();
        employeeRepository.setShouldFail(true);

        await viewModel.saveIdCard(newIdCard);

        expect(viewModel.idCardStatus, SectionLoadStatus.error);
      });
    });

    group('vote id section', () {
      test('loadVoteId transitions from notLoaded to loaded with data',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setVoteId(
            const EmployeeVoteId(number: '123456789012'));

        await viewModel.loadVoteId();

        expect(viewModel.voteIdStatus, SectionLoadStatus.loaded);
        expect(viewModel.voteId?.number, '123456789012');
      });

      test('loadVoteId sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);

        await viewModel.loadVoteId();

        expect(viewModel.voteIdStatus, SectionLoadStatus.error);
      });

      test('saveVoteId persists new number and shows a snack message',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadVoteId();

        await viewModel.saveVoteId('999999999999');

        expect(viewModel.voteIdStatus, SectionLoadStatus.loaded);
        expect(viewModel.voteId?.number, '999999999999');
        expect(viewModel.snackMessage, contains('eleitor'));
        expect(employeeRepository.lastSavedVoteIdNumber, '999999999999');
      });

      test('saveVoteId sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadVoteId();
        employeeRepository.setShouldFail(true);

        await viewModel.saveVoteId('999999999999');

        expect(viewModel.voteIdStatus, SectionLoadStatus.error);
      });
    });

    group('military document section', () {
      const fakeDoc = EmployeeMilitaryDocument(
        number: 'RM-12345',
        type: 'Reservista',
        isRequired: true,
      );

      test('loadMilitaryDocument transitions from notLoaded to loaded with data',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setMilitaryDocument(fakeDoc);

        await viewModel.loadMilitaryDocument();

        expect(viewModel.militaryDocumentStatus, SectionLoadStatus.loaded);
        expect(viewModel.militaryDocument?.number, 'RM-12345');
        expect(viewModel.militaryDocument?.type, 'Reservista');
        expect(viewModel.militaryDocument?.isRequired, true);
      });

      test(
          'loadMilitaryDocument sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);

        await viewModel.loadMilitaryDocument();

        expect(viewModel.militaryDocumentStatus, SectionLoadStatus.error);
      });

      test(
          'saveMilitaryDocument persists new values and shows a snack message',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadMilitaryDocument();

        await viewModel.saveMilitaryDocument('RM-99999', 'Certificado');

        expect(viewModel.militaryDocumentStatus, SectionLoadStatus.loaded);
        expect(viewModel.militaryDocument?.number, 'RM-99999');
        expect(viewModel.militaryDocument?.type, 'Certificado');
        expect(viewModel.snackMessage, contains('militar'));
        expect(employeeRepository.lastSavedMilitaryDocumentNumber, 'RM-99999');
        expect(employeeRepository.lastSavedMilitaryDocumentType, 'Certificado');
      });

      test(
          'saveMilitaryDocument sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadMilitaryDocument();
        employeeRepository.setShouldFail(true);

        await viewModel.saveMilitaryDocument('RM-99999', 'Certificado');

        expect(viewModel.militaryDocumentStatus, SectionLoadStatus.error);
      });
    });

    group('medical exam section', () {
      const fakeExam = EmployeeMedicalExam(
        dateExam: '15/01/2026',
        validityExam: '15/01/2027',
      );

      test('loadMedicalExam transitions from notLoaded to loaded with data',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setMedicalExam(fakeExam);

        await viewModel.loadMedicalExam();

        expect(viewModel.medicalExamStatus, SectionLoadStatus.loaded);
        expect(viewModel.medicalExam?.dateExam, '15/01/2026');
        expect(viewModel.medicalExam?.validityExam, '15/01/2027');
      });

      test('loadMedicalExam sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);

        await viewModel.loadMedicalExam();

        expect(viewModel.medicalExamStatus, SectionLoadStatus.error);
      });

      test('saveMedicalExam persists new values and shows a snack message',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadMedicalExam();

        await viewModel.saveMedicalExam('20/03/2026', '20/03/2027');

        expect(viewModel.medicalExamStatus, SectionLoadStatus.loaded);
        expect(viewModel.medicalExam?.dateExam, '20/03/2026');
        expect(viewModel.medicalExam?.validityExam, '20/03/2027');
        expect(viewModel.snackMessage, contains('médico'));
        expect(employeeRepository.lastSavedMedicalExamDate, '20/03/2026');
        expect(employeeRepository.lastSavedMedicalExamValidity, '20/03/2027');
      });

      test('saveMedicalExam sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadMedicalExam();
        employeeRepository.setShouldFail(true);

        await viewModel.saveMedicalExam('20/03/2026', '20/03/2027');

        expect(viewModel.medicalExamStatus, SectionLoadStatus.error);
      });
    });

    group('role info section', () {
      final fakeDepartments = [
        Department(
          id: 'dept-1',
          name: 'Financeiro',
          description: 'Setor financeiro',
          positions: [
            Position(
              id: 'pos-1',
              name: 'Analista',
              description: 'Analista financeiro',
              cbo: '251210',
              roles: [_fakeRole],
            ),
          ],
        ),
      ];

      test('loadRoleInfo transitions to loaded and finds current role',
          () async {
        departmentRepository.setDepartments(fakeDepartments);
        await viewModel.load('emp-1');

        await viewModel.loadRoleInfo();

        expect(viewModel.roleInfoStatus, SectionLoadStatus.loaded);
        expect(viewModel.allDepartments, hasLength(1));
        expect(viewModel.currentDepartmentId, 'dept-1');
        expect(viewModel.currentPositionId, 'pos-1');
      });

      test('loadRoleInfo sets error status when the repository call fails',
          () async {
        departmentRepository.setShouldFail(true);
        await viewModel.load('emp-1');

        await viewModel.loadRoleInfo();

        expect(viewModel.roleInfoStatus, SectionLoadStatus.error);
      });

      test('saveEmployeeRole persists new roleId and shows a snack message',
          () async {
        departmentRepository.setDepartments(fakeDepartments);
        await viewModel.load('emp-1');
        await viewModel.loadRoleInfo();

        await viewModel.saveEmployeeRole('role-1');

        expect(viewModel.roleInfoStatus, SectionLoadStatus.loaded);
        expect(viewModel.snackMessage, contains('Função'));
        expect(employeeRepository.lastSavedRoleId, 'role-1');
      });

      test(
          'saveEmployeeRole sets error status when the repository call fails',
          () async {
        departmentRepository.setDepartments(fakeDepartments);
        await viewModel.load('emp-1');
        await viewModel.loadRoleInfo();
        employeeRepository.setShouldFail(true);

        await viewModel.saveEmployeeRole('role-1');

        expect(viewModel.roleInfoStatus, SectionLoadStatus.error);
      });
    });

    // ─── Validators ──────────────────────────────────────────────────────────

    group('validators', () {
      group('validatePhone', () {
        test('returns null for empty input', () {
          expect(viewModel.validatePhone(''), isNull);
          expect(viewModel.validatePhone(null), isNull);
        });

        test('returns null for valid 11-digit phone', () {
          expect(viewModel.validatePhone('11987654321'), isNull);
        });

        test('returns error for invalid digit count', () {
          expect(viewModel.validatePhone('1234'), isNotNull);
        });
      });

      group('validateEmail', () {
        test('returns null for empty input', () {
          expect(viewModel.validateEmail(''), isNull);
        });

        test('returns null for valid email', () {
          expect(viewModel.validateEmail('test@example.com'), isNull);
        });

        test('returns error for invalid email', () {
          expect(viewModel.validateEmail('not-an-email'), isNotNull);
        });
      });

      group('validateCep', () {
        test('returns error for empty input', () {
          expect(viewModel.validateCep(''), isNotNull);
        });

        test('returns null for valid 8-digit CEP', () {
          expect(viewModel.validateCep('01310-100'), isNull);
        });

        test('returns error for short CEP', () {
          expect(viewModel.validateCep('1234'), isNotNull);
        });
      });

      group('isCpfValid', () {
        test('returns true for a mathematically valid CPF', () {
          expect(viewModel.isCpfValid('111.444.777-35'), isTrue);
        });

        test('returns false for all-same-digit CPF', () {
          expect(viewModel.isCpfValid('000.000.000-00'), isFalse);
          expect(viewModel.isCpfValid('111.111.111-11'), isFalse);
        });

        test('returns false for wrong check digits', () {
          expect(viewModel.isCpfValid('123.456.789-01'), isFalse);
        });
      });

      group('validateCpf', () {
        test('returns error for empty input', () {
          expect(viewModel.validateCpf(''), isNotNull);
        });

        test('returns null for valid CPF', () {
          expect(viewModel.validateCpf('111.444.777-35'), isNull);
        });

        test('returns error for invalid CPF algorithm', () {
          expect(viewModel.validateCpf('12345678901'), isNotNull);
        });
      });

      group('validateDateOfBirth', () {
        test('returns error for empty input', () {
          expect(viewModel.validateDateOfBirth(''), isNotNull);
        });

        test('returns null for a valid past date', () {
          expect(viewModel.validateDateOfBirth('15/06/1990'), isNull);
        });

        test('returns error for a future date', () {
          expect(viewModel.validateDateOfBirth('01/01/2099'), isNotNull);
        });
      });

      group('isVoteIdValid', () {
        test('returns true for a mathematically valid vote ID', () {
          expect(viewModel.isVoteIdValid('123456780698'), isTrue);
        });

        test('returns false for all-same-digit vote ID', () {
          expect(viewModel.isVoteIdValid('111111111111'), isFalse);
        });

        test('returns false for wrong check digits', () {
          expect(viewModel.isVoteIdValid('123456789012'), isFalse);
        });
      });

      group('validateVoteIdNumber', () {
        test('returns error for empty input', () {
          expect(viewModel.validateVoteIdNumber(''), isNotNull);
        });

        test('returns null for valid vote ID', () {
          expect(viewModel.validateVoteIdNumber('1234.5678.0698'), isNull);
        });

        test('returns error for invalid vote ID', () {
          expect(viewModel.validateVoteIdNumber('1234.5678.9012'), isNotNull);
        });
      });

      group('validateMilitaryNumber', () {
        test('returns error for empty input', () {
          expect(viewModel.validateMilitaryNumber(''), isNotNull);
        });

        test('returns null for valid number', () {
          expect(viewModel.validateMilitaryNumber('RM-12345'), isNull);
        });

        test('returns error for number exceeding 20 characters', () {
          expect(
            viewModel.validateMilitaryNumber('A' * 21),
            isNotNull,
          );
        });
      });

      group('validateMilitaryType', () {
        test('returns error for empty input', () {
          expect(viewModel.validateMilitaryType(''), isNotNull);
        });

        test('returns null for valid type', () {
          expect(viewModel.validateMilitaryType('Reservista'), isNull);
        });

        test('returns error for type exceeding 50 characters', () {
          expect(viewModel.validateMilitaryType('A' * 51), isNotNull);
        });
      });

      group('validateDateExam', () {
        test('returns error for empty input', () {
          expect(viewModel.validateDateExam(''), isNotNull);
        });

        test('returns error for date older than 365 days', () {
          expect(viewModel.validateDateExam('01/01/2020'), isNotNull);
        });
      });

      group('validateExamValidity', () {
        test('returns error for empty input', () {
          expect(viewModel.validateExamValidity(''), isNotNull);
        });

        test('returns error for past date', () {
          expect(viewModel.validateExamValidity('01/01/2020'), isNotNull);
        });
      });

      group('formatSalary', () {
        test('returns formatted salary with type, value, and unit', () {
          expect(viewModel.formatSalary(_fakeRole), contains('3500.00'));
        });
      });

      group('validateDependentName', () {
        test('returns error for empty input', () {
          expect(viewModel.validateDependentName(''), isNotNull);
        });

        test('returns null for valid name', () {
          expect(viewModel.validateDependentName('Maria Silva'), isNull);
        });

        test('returns error for name exceeding 100 characters', () {
          expect(viewModel.validateDependentName('A' * 101), isNotNull);
        });
      });
    });

    group('dependents section', () {
      test('loadDependents transitions from notLoaded to loaded with data',
          () async {
        await viewModel.load('emp-1');

        await viewModel.loadDependents();

        expect(viewModel.dependentsStatus, SectionLoadStatus.loaded);
        expect(viewModel.dependents, hasLength(1));
        expect(viewModel.dependents.first.name, 'Maria Silva');
      });

      test('loadDependents sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);

        await viewModel.loadDependents();

        expect(viewModel.dependentsStatus, SectionLoadStatus.error);
      });

      test('removeDependent removes the dependent and shows a snack message',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadDependents();

        await viewModel.removeDependent('Maria Silva');

        expect(viewModel.dependentsStatus, SectionLoadStatus.loaded);
        expect(viewModel.dependents, isEmpty);
        expect(viewModel.snackMessage, contains('removido'));
        expect(employeeRepository.lastRemovedDependentName, 'Maria Silva');
      });

      test('removeDependent sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadDependents();
        employeeRepository.setShouldFail(true);

        await viewModel.removeDependent('Maria Silva');

        expect(viewModel.dependentsStatus, SectionLoadStatus.error);
      });
    });

    group('workplace info section', () {
      test('loadWorkplaceInfo transitions to loaded with data', () async {
        workplaceRepository.setWorkplaces([_fakeWorkplace]);
        await viewModel.load('emp-1');

        await viewModel.loadWorkplaceInfo();

        expect(viewModel.workplaceInfoStatus, SectionLoadStatus.loaded);
        expect(viewModel.allWorkplaces, hasLength(1));
        expect(viewModel.allWorkplaces.first.name, 'Sede Principal');
      });

      test(
          'loadWorkplaceInfo sets error status when the repository call fails',
          () async {
        workplaceRepository.setShouldFail(true);
        await viewModel.load('emp-1');

        await viewModel.loadWorkplaceInfo();

        expect(viewModel.workplaceInfoStatus, SectionLoadStatus.error);
      });

      test(
          'saveEmployeeWorkplace persists new workplaceId and shows a snack message',
          () async {
        workplaceRepository.setWorkplaces([_fakeWorkplace]);
        await viewModel.load('emp-1');
        await viewModel.loadWorkplaceInfo();

        await viewModel.saveEmployeeWorkplace('wp-1');

        expect(viewModel.workplaceInfoStatus, SectionLoadStatus.loaded);
        expect(viewModel.snackMessage, contains('trabalho'));
        expect(employeeRepository.lastSavedWorkplaceId, 'wp-1');
      });

      test(
          'saveEmployeeWorkplace sets error status when the repository call fails',
          () async {
        workplaceRepository.setWorkplaces([_fakeWorkplace]);
        await viewModel.load('emp-1');
        await viewModel.loadWorkplaceInfo();
        employeeRepository.setShouldFail(true);

        await viewModel.saveEmployeeWorkplace('wp-1');

        expect(viewModel.workplaceInfoStatus, SectionLoadStatus.error);
      });
    });

    group('contracts section', () {
      test('loadContracts transitions to loaded with data', () async {
        await viewModel.load('emp-1');

        await viewModel.loadContracts();

        expect(viewModel.contractsStatus, SectionLoadStatus.loaded);
        expect(viewModel.contracts, hasLength(1));
        expect(viewModel.contracts.first.typeName, 'CLT');
        expect(viewModel.contracts.first.isActive, isTrue);
        expect(viewModel.contractTypes, hasLength(2));
      });

      test('loadContracts sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);

        await viewModel.loadContracts();

        expect(viewModel.contractsStatus, SectionLoadStatus.error);
      });

      test('validateContractDate returns null for a valid recent date', () {
        // Today's date should be valid (within ±365 days).
        final now = DateTime.now();
        final formatted =
            '${now.day.toString().padLeft(2, '0')}/${now.month.toString().padLeft(2, '0')}/${now.year}';
        expect(viewModel.validateContractDate(formatted), isNull);
      });

      test('validateContractDate returns error for empty input', () {
        expect(viewModel.validateContractDate(''), isNotNull);
      });

      test('validateContractDate returns error for a date beyond ±365 days',
          () {
        expect(viewModel.validateContractDate('01/01/2020'), isNotNull);
      });
    });

    group('signing options section', () {
      test('loadSigningOptions transitions to loaded with data', () async {
        await viewModel.load('emp-1');

        await viewModel.loadSigningOptions();

        expect(viewModel.signingOptionsStatus, SectionLoadStatus.loaded);
        expect(viewModel.signingOptions, hasLength(2));
      });

      test(
          'loadSigningOptions sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        employeeRepository.setShouldFail(true);

        await viewModel.loadSigningOptions();

        expect(viewModel.signingOptionsStatus, SectionLoadStatus.error);
      });

      test('saveSigningOption persists option and shows a snack message',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadSigningOptions();

        await viewModel.saveSigningOption('2');

        expect(viewModel.signingOptionsStatus, SectionLoadStatus.loaded);
        expect(viewModel.profile?.documentSigningOptionsId, '2');
        expect(viewModel.snackMessage, contains('assinatura'));
      });

      test(
          'saveSigningOption sets error status when the repository call fails',
          () async {
        await viewModel.load('emp-1');
        await viewModel.loadSigningOptions();
        employeeRepository.setShouldFail(true);

        await viewModel.saveSigningOption('2');

        expect(viewModel.signingOptionsStatus, SectionLoadStatus.error);
      });
    });
  });
}
