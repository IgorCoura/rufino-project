import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/domain/services/company_service.dart';
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
import 'package:rufino/modules/employee/domain/role/role.dart';
import 'package:rufino/modules/employee/domain/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'employee_profile_event.dart';
part 'employee_profile_state.dart';

class EmployeeProfileBloc
    extends Bloc<EmployeeProfileEvent, EmployeeProfileState> {
  final CompanyService _companyService;
  final PeopleManagementService _peopleManagementService;

  EmployeeProfileBloc(this._companyService, this._peopleManagementService)
      : super(const EmployeeProfileState()) {
    on<InitialEmployeeProfileEvent>(_onInitialEvent);
    on<EditNameEvent>(_onEditNameEvent);
    on<ChangeNameEvent>(_onChangeNameEvent);
    on<LoadingContactEvent>(_onLoadingContactEvent);
    on<SaveNewNameEvent>(_onSaveNewNameEvent);
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
  }

  Future _onInitialEvent(InitialEmployeeProfileEvent event,
      Emitter<EmployeeProfileState> emit) async {
    emit(const EmployeeProfileState(isLoading: true));
    try {
      var company = await _companyService.getSelectedCompany();
      var employee = await _peopleManagementService.getEmployee(
          event.employeeId, company.id);
      var role =
          await _peopleManagementService.getRole(company.id, employee.roleId);
      if (state.militaryDocument.isLoading) {
        var militaryDocument = await _peopleManagementService
            .getEmployeeMilitaryDocument(employee.id, company.id);

        emit(state.copyWith(militaryDocument: militaryDocument));
      }

      emit(state.copyWith(
          company: company, employee: employee, role: role, isLoading: false));
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
}
