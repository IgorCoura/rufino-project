import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/employee/domain/model/document_signing_options.dart';
import 'package:rufino/modules/employee/domain/model/employee_contract.dart';
import 'package:rufino/modules/employee/domain/model/employee_contract_type.dart';
import 'package:rufino/modules/employee/domain/model/medical_admission_exam/medical_admission_exam.dart';
import 'package:rufino/modules/employee/domain/model/military_document/military_document.dart';
import 'package:rufino/modules/employee/domain/model/vote_id/vote_id.dart';
import 'package:rufino/modules/employee/domain/model/address/address.dart';
import 'package:rufino/modules/employee/domain/model/contact/contact.dart';
import 'package:rufino/modules/employee/domain/model/employee.dart';
import 'package:rufino/modules/employee/domain/model/name.dart';
import 'package:rufino/modules/employee/domain/model/id_card/id_card.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/personal_info.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/personal_info_seletion_options.dart';
import 'package:rufino/modules/employee/domain/model/status.dart';
import 'package:rufino/modules/employee/domain/model/workplace/workplace.dart';
import 'package:rufino/modules/employee/domain/model/role_info/department.dart';
import 'package:rufino/modules/employee/domain/model/role_info/position.dart';
import 'package:rufino/modules/employee/domain/model/role_info/role.dart';
import 'package:rufino/modules/employee/domain/model/role_info/role_info.dart';
import 'package:rufino/modules/employee/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'employee_profile_event.dart';
part 'employee_profile_state.dart';

class EmployeeProfileBloc
    extends Bloc<EmployeeProfileEvent, EmployeeProfileState> {
  final CompanyGlobalService _companyService;
  final PeopleManagementService _peopleManagementService;

  EmployeeProfileBloc(this._companyService, this._peopleManagementService)
      : super(const EmployeeProfileState()) {
    on<InitialEmployeeProfileEvent>(_onInitialEvent);
    on<EditNameEvent>(_onEditNameEvent);
    on<ChangeNameEvent>(_onChangeNameEvent);
    on<SaveNewNameEvent>(_onSaveNewNameEvent);
    on<EditDocumentSigningOptionsEvent>(_onEditDocumentSigningOptionsEvent);
    on<ChangeDocumentSigningOptionsEvent>(_onChangeDocumentSigningOptionsEvent);
    on<SaveDocumentSigningOptionsEvent>(_onSaveDocumentSigningOptionsEvent);
    on<LoadingContactEvent>(_onLoadingContactEvent);
    on<SnackMessageWasShow>(_onSnackMessageWasShow);
    on<SaveContactChanges>(_onSaveContactChanges);
    on<LoadingAddressEvent>(_onLoadingAddressEvent);
    on<SaveAddressEvent>(_onSaveAddressEvent);
    on<LoadingPersonalInfoEvent>(_onLoadingPersonalInfoEvent);
    on<SavePersonalInfoEvent>(_onSavePersonalInfoEvent);
    on<LazyLoadingPersonalInfoEvent>(_onLazyLoadingPersonalInfoEvent);
    on<LoadingIdCardEvent>(_onLoadingIdCardEvent);
    on<SaveIdCardEvent>(_onSaveIdCardEvent);
    on<LoadingVoteIdEvent>(_onLoadingVoteIdEvent);
    on<SaveVoteIdEvent>(_onSaveVoteIdEvent);
    on<LoadingMilitaryDocumentEvent>(_onLoadingMilitaryDocumentEvent);
    on<SaveMilitaryDocumentEvent>(_onSaveMilitaryDocumentEvent);
    on<LoadingMedicalAdmissionExamEvent>(_onLoadingMedicalAdmissionExamEvent);
    on<SaveMedicalAdmissionExamEvent>(_onSaveMedicalAdmissionExamEvent);
    on<LoadingRoleEvent>(_onLoadingRoleEvent);
    on<LazyLoadingRoleInfoEvent>(_onLazyLoadingRoleInfoEvent);
    on<ChangeRoleInfoEvent>(_onChangeRoleInfoEvent);
    on<SaveRoleInfoEvent>(_onSaveRoleInfoEvent);
    on<LoadingWorkplaceEvent>(_onLoadingWorkplaceEvent);
    on<LazyLoadingWorkplaceEvent>(_onLazyLoadingWorkplaceEvent);
    on<ChangeWorkplaceEvent>(_onChangeWorkplaceEvent);
    on<SaveWorkplaceEvent>(_onSaveWorkplaceEvent);
    on<LoadingContractsEvent>(_onLoadingContractsEvent);
    on<FinishedContractEvent>(_onFinishedContractEvent);
    on<NewContractEvent>(_onNewContractEvent);
  }

  Future _onInitialEvent(InitialEmployeeProfileEvent event,
      Emitter<EmployeeProfileState> emit) async {
    emit(const EmployeeProfileState(isLoading: true));
    try {
      var company = await _companyService.getSelectedCompany();
      var employee = await _peopleManagementService.getEmployee(
          event.employeeId, company.id);
      var documentSigningOptions =
          await _peopleManagementService.getDocumentSigningOptions(company.id);
      var contractsType =
          await _peopleManagementService.getEmployeeContractTypes(company.id);

      if (state.militaryDocument.isLoading) {
        var militaryDocument = await _peopleManagementService
            .getEmployeeMilitaryDocument(employee.id, company.id);

        emit(state.copyWith(militaryDocument: militaryDocument));
      }

      emit(state.copyWith(
          company: company,
          employee: employee,
          listContractTypes: contractsType,
          listDocumentSigningOptions: documentSigningOptions,
          isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onEditNameEvent(
      EditNameEvent event, Emitter<EmployeeProfileState> emit) {
    emit(state.copyWith(isEditingName: event.edit));
  }

  void _onChangeNameEvent(
      ChangeNameEvent event, Emitter<EmployeeProfileState> emit) {
    emit(
      state.copyWith(
        employee: state.employee.copyWith(
          name: Name(event.name),
        ),
      ),
    );
  }

  Future _onSaveNewNameEvent(
      SaveNewNameEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));
      await _peopleManagementService.editEmployeeName(
          state.employee.id, state.company.id, state.employee.name.value);
      emit(state.copyWith(
          isSavingData: false,
          isEditingName: false,
          snackMessage:
              "${state.employee.name.displayName} alterado com sucesso."));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  void _onEditDocumentSigningOptionsEvent(EditDocumentSigningOptionsEvent event,
      Emitter<EmployeeProfileState> emit) {
    emit(state.copyWith(isDocumentSigningOptionsName: event.edit));
  }

  void _onChangeDocumentSigningOptionsEvent(
      ChangeDocumentSigningOptionsEvent event,
      Emitter<EmployeeProfileState> emit) {
    emit(
      state.copyWith(
        employee: state.employee.copyWith(
          documentSigningOptions: event.option,
        ),
      ),
    );
  }

  Future _onSaveDocumentSigningOptionsEvent(
      SaveDocumentSigningOptionsEvent event,
      Emitter<EmployeeProfileState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));

      await _peopleManagementService.editDocumentSigningOptions(
          state.employee.id,
          state.company.id,
          state.employee.documentSigningOptions);

      emit(state.copyWith(
          isSavingData: false,
          isEditingName: false,
          snackMessage:
              "${state.employee.documentSigningOptions.displayName} alterado com sucesso."));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  void _onSnackMessageWasShow(
      SnackMessageWasShow event, Emitter<EmployeeProfileState> emit) {
    emit(state.copyWith(snackMessage: ""));
  }

  Future _onLoadingContactEvent(
      LoadingContactEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      if (state.contact.isLoading) {
        var contact = await _peopleManagementService.getEmployeeContact(
            state.employee.id, state.company.id);
        emit(state.copyWith(contact: contact));
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onSaveContactChanges(
      SaveContactChanges event, Emitter<EmployeeProfileState> emit) async {
    try {
      var newContact = state.contact.copyWith();
      for (var change in event.changes) {
        newContact = newContact.copyWith(generic: change);
      }

      emit(state.copyWith(isSavingData: true, contact: newContact));

      await _peopleManagementService.editEmployeeContact(state.employee.id,
          state.company.id, newContact.cellphone.value, newContact.email.value);

      emit(state.copyWith(
          isSavingData: false,
          isEditingName: false,
          snackMessage: "Contato alterado com sucesso."));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onLoadingAddressEvent(
      LoadingAddressEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      if (state.address.isLoading) {
        var address = await _peopleManagementService.getEmployeeAddress(
            state.employee.id, state.company.id);
        emit(state.copyWith(address: address as Address?));
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onSaveAddressEvent(
      SaveAddressEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      var newAdress = state.address.copyWith();
      for (var change in event.changes) {
        newAdress = newAdress.copyWith(generic: change);
      }

      emit(state.copyWith(isSavingData: true, address: newAdress));

      await _peopleManagementService.editEmployeeAddress(
          state.employee.id, state.company.id, newAdress);

      emit(state.copyWith(
          isSavingData: false,
          isEditingName: false,
          snackMessage: "Endereço alterado com sucesso."));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onLoadingPersonalInfoEvent(LoadingPersonalInfoEvent event,
      Emitter<EmployeeProfileState> emit) async {
    try {
      if (state.personalInfo.isLoading) {
        var personalInfo = await _peopleManagementService
            .getEmployeePersonalInfo(state.employee.id, state.company.id);

        emit(state.copyWith(personalInfo: personalInfo));
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onSavePersonalInfoEvent(
      SavePersonalInfoEvent event, Emitter<EmployeeProfileState> emit) async {
    var newPersonalInfo = state.personalInfo.copyWith();
    for (var change in event.changes) {
      newPersonalInfo = newPersonalInfo.copyWith(generic: change);
    }
    emit(state.copyWith(isSavingData: true, personalInfo: newPersonalInfo));

    await _peopleManagementService.editEmployeePersonalInfo(
        state.employee.id, state.company.id, newPersonalInfo);
    emit(state.copyWith(
        isSavingData: false,
        isEditingName: false,
        snackMessage: "Informações pessoais alterado com sucesso."));
  }

  Future _onLazyLoadingPersonalInfoEvent(LazyLoadingPersonalInfoEvent event,
      Emitter<EmployeeProfileState> emit) async {
    emit(state.copyWith(
        personalInfo: state.personalInfo.copyWith(isLazyLoading: true)));
    var personalInfoSelectionOptions = await _peopleManagementService
        .getPersonalInfoSeletionOptions(state.company.id);

    emit(state.copyWith(
      personalInfoSeletionOptions: personalInfoSelectionOptions,
      personalInfo: state.personalInfo.copyWith(isLazyLoading: true),
    ));
  }

  Future _onLoadingIdCardEvent(
      LoadingIdCardEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      if (state.idCard.isLoading) {
        var idCard = await _peopleManagementService.getEmployeeIdCard(
            state.employee.id, state.company.id);

        emit(state.copyWith(idCard: idCard));
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onSaveIdCardEvent(
      SaveIdCardEvent event, Emitter<EmployeeProfileState> emit) async {
    var newIdcard = state.idCard.copyWith();
    for (var change in event.changes) {
      newIdcard = newIdcard.copyWith(generic: change);
    }
    emit(state.copyWith(isSavingData: true, idCard: newIdcard));

    await _peopleManagementService.editEmployeeIdCard(
        state.employee.id, state.company.id, newIdcard);

    emit(state.copyWith(
        isSavingData: false,
        isEditingName: false,
        snackMessage: "Identidade alterado com sucesso."));
  }

  Future _onLoadingVoteIdEvent(
      LoadingVoteIdEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      if (state.voteId.isLoading) {
        var voteId = await _peopleManagementService.getEmployeeVoteId(
            state.employee.id, state.company.id);

        emit(state.copyWith(voteId: voteId));
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);

      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onSaveVoteIdEvent(
      SaveVoteIdEvent event, Emitter<EmployeeProfileState> emit) async {
    var newVoteId = state.voteId.copyWith();
    for (var change in event.changes) {
      newVoteId = newVoteId.copyWith(generic: change);
    }
    emit(state.copyWith(isSavingData: true, voteId: newVoteId));

    await _peopleManagementService.editEmployeeVoteId(
        state.employee.id, state.company.id, newVoteId);

    emit(state.copyWith(
        isSavingData: false,
        isEditingName: false,
        snackMessage: "Título de eleitor alterado com sucesso."));
  }

  Future _onLoadingMilitaryDocumentEvent(LoadingMilitaryDocumentEvent event,
      Emitter<EmployeeProfileState> emit) async {
    try {
      if (state.militaryDocument.isLoading) {
        var militaryDocument = await _peopleManagementService
            .getEmployeeMilitaryDocument(state.employee.id, state.company.id);

        emit(state.copyWith(militaryDocument: militaryDocument));
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onSaveMilitaryDocumentEvent(SaveMilitaryDocumentEvent event,
      Emitter<EmployeeProfileState> emit) async {
    var newMilitaryDocument = state.militaryDocument.copyWith();
    for (var change in event.changes) {
      newMilitaryDocument = newMilitaryDocument.copyWith(generic: change);
    }
    emit(state.copyWith(
        isSavingData: true, militaryDocument: newMilitaryDocument));

    await _peopleManagementService.editEmployeeMilitaryDocument(
        state.employee.id, state.company.id, newMilitaryDocument);

    emit(state.copyWith(
        isSavingData: false,
        isEditingName: false,
        snackMessage: "Documento Militar alterado com sucesso."));
  }

  Future _onLoadingMedicalAdmissionExamEvent(
      LoadingMedicalAdmissionExamEvent event,
      Emitter<EmployeeProfileState> emit) async {
    try {
      if (state.medicalAdmissionExam.isLoading) {
        var exam =
            await _peopleManagementService.getEmployeeMedicalAdmissionExam(
                state.employee.id, state.company.id);

        emit(state.copyWith(medicalAdmissionExam: exam));
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onSaveMedicalAdmissionExamEvent(SaveMedicalAdmissionExamEvent event,
      Emitter<EmployeeProfileState> emit) async {
    var newExam = state.medicalAdmissionExam.copyWith();
    for (var change in event.changes) {
      newExam = newExam.copyWith(generic: change);
    }
    emit(state.copyWith(isSavingData: true, medicalAdmissionExam: newExam));

    await _peopleManagementService.editMedicalAdmissionExam(
        state.employee.id, state.company.id, newExam);

    emit(state.copyWith(
        isSavingData: false,
        isEditingName: false,
        snackMessage: "Exame medico admissional foi alterado com sucesso."));
  }

  Future _onLoadingRoleEvent(
      LoadingRoleEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      var roleInfo = await _peopleManagementService.getRole(
          state.company.id, state.employee.roleId);

      emit(state.copyWith(roleInfo: roleInfo));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onLazyLoadingRoleInfoEvent(LazyLoadingRoleInfoEvent event,
      Emitter<EmployeeProfileState> emit) async {
    emit(
        state.copyWith(roleInfo: state.roleInfo.copyWith(isLazyLoading: true)));

    var departments =
        await _peopleManagementService.getAllDepartment(state.company.id);
    List<Position> positons = [];
    if (state.roleInfo.department != const Department.empty()) {
      positons = await _peopleManagementService.getAllPosition(
          state.roleInfo.department.id, state.company.id);
    }
    List<Role> roles = [];
    if (state.roleInfo.position != const Position.empty()) {
      roles = await _peopleManagementService.getAllRole(
          state.roleInfo.position.id, state.company.id);
    }

    emit(state.copyWith(
        listDepartment: departments,
        listPosition: positons,
        listRolen: roles,
        roleInfo: state.roleInfo.copyWith(isLazyLoading: false)));
  }

  Future _onChangeRoleInfoEvent(
      ChangeRoleInfoEvent event, Emitter<EmployeeProfileState> emit) async {
    var change = event.change;
    if (event.change is Department) {
      var newDepartment = change as Department;
      var positons = await _peopleManagementService.getAllPosition(
          newDepartment.id, state.company.id);
      emit(state.copyWith(
          listPosition: positons,
          roleInfo: state.roleInfo.copyWith(
              depertment: newDepartment,
              position: const Position.empty(),
              role: const Role.empty())));
    }
    if (event.change is Position) {
      var newPsoition = change as Position;
      var roles = await _peopleManagementService.getAllRole(
          newPsoition.id, state.company.id);
      emit(state.copyWith(
          listRolen: roles,
          roleInfo: state.roleInfo
              .copyWith(position: newPsoition, role: const Role.empty())));
    }
    if (event.change is Role) {
      emit(state.copyWith(
          roleInfo: state.roleInfo.copyWith(role: change as Role)));
    }
  }

  Future _onSaveRoleInfoEvent(
      SaveRoleInfoEvent event, Emitter<EmployeeProfileState> emit) async {
    emit(state.copyWith(isSavingData: true));

    if (state.roleInfo.role != const Role.empty()) {
      await _peopleManagementService.editRoleInfo(
          state.employee.id, state.company.id, state.roleInfo.role.id);
      emit(state.copyWith(
          employee: state.employee.copyWith(roleId: state.roleInfo.role.id),
          isSavingData: false,
          isEditingName: false,
          snackMessage: "As Informações de Função foi alterado com sucesso."));
    } else {
      emit(state.copyWith(
          isSavingData: false,
          isEditingName: false,
          snackMessage: "Por favor, selecione um função valida."));
    }
  }

  Future _onLoadingWorkplaceEvent(
      LoadingWorkplaceEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      var workplace = await _peopleManagementService.getWorkplaceById(
          state.employee.workplaceId, state.company.id);
      emit(state.copyWith(workplace: workplace));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onLazyLoadingWorkplaceEvent(LazyLoadingWorkplaceEvent event,
      Emitter<EmployeeProfileState> emit) async {
    var workplaces =
        await _peopleManagementService.getAllWorkplace(state.company.id);
    emit(state.copyWith(
      listWorkplace: workplaces,
    ));
  }

  Future _onChangeWorkplaceEvent(
      ChangeWorkplaceEvent event, Emitter<EmployeeProfileState> emit) async {
    var workplace = event.change as Workplace;
    emit(state.copyWith(workplace: workplace));
  }

  Future _onSaveWorkplaceEvent(
      SaveWorkplaceEvent event, Emitter<EmployeeProfileState> emit) async {
    emit(state.copyWith(isSavingData: true));

    await _peopleManagementService.editWorkplace(
        state.employee.id, state.company.id, state.workplace.id);

    emit(state.copyWith(
        employee: state.employee.copyWith(workplaceId: state.workplace.id),
        isSavingData: false,
        isEditingName: false,
        snackMessage: "Local de trabalho alterado com sucesso."));
  }

  Future _onLoadingContractsEvent(
      LoadingContractsEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      var contracts = await _peopleManagementService.getEmployeeContracts(
          state.employee.id, state.company.id);
      var contractsType = await _peopleManagementService
          .getEmployeeContractTypes(state.company.id);
      emit(state.copyWith(
          listContracts: contracts, listContractTypes: contractsType));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onFinishedContractEvent(
      FinishedContractEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));

      await _peopleManagementService.finishedContract(
          state.employee.id, state.company.id, event.finalDate);

      add(LoadingContractsEvent());
      emit(state.copyWith(isSavingData: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }

  Future _onNewContractEvent(
      NewContractEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));

      await _peopleManagementService.newContract(
          state.employee.id,
          state.company.id,
          event.initDate,
          event.contractTypeId,
          event.registration);

      add(LoadingContractsEvent());
      add(InitialEmployeeProfileEvent(state.employee.id));
      emit(state.copyWith(isSavingData: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, isSavingData: false, exception: exception));
    }
  }
}
