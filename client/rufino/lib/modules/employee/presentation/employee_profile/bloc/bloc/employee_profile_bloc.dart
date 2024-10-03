import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/domain/services/company_service.dart';
import 'package:rufino/modules/employee/domain/model/Address/Address.dart';
import 'package:rufino/modules/employee/domain/model/contact/contact.dart';
import 'package:rufino/modules/employee/domain/model/employee.dart';
import 'package:rufino/modules/employee/domain/model/name.dart';
import 'package:rufino/modules/employee/domain/model/status.dart';
import 'package:rufino/modules/employee/domain/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'employee_profile_event.dart';
part 'employee_profile_state.dart';

class EmployeeProfileBloc
    extends Bloc<EmployeeProfileEvent, EmployeeProfileState> {
  final CompanyService _companyService;
  final PeopleManagementService _peopleManagementService;

  EmployeeProfileBloc(this._companyService, this._peopleManagementService)
      : super(EmployeeProfileState(employee: Employee.empty)) {
    on<InitialEmployeeProfileEvent>(_onInitialEvent);
    on<EditNameEvent>(_onEditNameEvent);
    on<ChangeNameEvent>(_onChangeNameEvent);
    on<LoadingContactEvent>(_onLoadingContactEvent);
    on<SaveNewNameEvent>(_onSaveNewNameEvent);
    on<SnackMessageWasShow>(_onSnackMessageWasShow);
    on<SaveContactChanges>(_onSaveContactChanges);
    on<LoadingAddressEvent>(_onLoadingAddressEvent);
    on<SaveAddressEvent>(_onSaveAddressEvent);
  }

  Future _onInitialEvent(InitialEmployeeProfileEvent event,
      Emitter<EmployeeProfileState> emit) async {
    emit(EmployeeProfileState(employee: Employee.empty, isLoading: true));
    try {
      var company = await _companyService.getSelectedCompany();
      var employee = await _peopleManagementService.getEmployee(
          event.employeeId, company.id);
      emit(state.copyWith(
          company: company, employee: employee, isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onEditNameEvent(
      EditNameEvent event, Emitter<EmployeeProfileState> emit) {
    emit(state.copyWith(isEditingName: true));
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

  Future _onLoadingContactEvent(
      LoadingContactEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      if (state.contact.isLoading) {
        var contact = await _peopleManagementService.getEmployeeContact(
            state.employee.id, state.company!.id);
        emit(state.copyWith(contact: contact));
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onSaveNewNameEvent(
      SaveNewNameEvent event, Emitter<EmployeeProfileState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));
      await _peopleManagementService.editEmployeeName(
          state.employee.id, state.company!.id, state.employee.name.value);
      emit(state.copyWith(
          isSavingData: false,
          isEditingName: false,
          snackMessage: "Nome alterado com sucesso."));
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

  Future _onSaveContactChanges(
      SaveContactChanges event, Emitter<EmployeeProfileState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));
      await _peopleManagementService.editEmployeeContact(
          state.employee.id,
          state.company!.id,
          state.contact.cellphone.value,
          state.contact.email.value);
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
            state.employee.id, state.company!.id);
        emit(state.copyWith(address: address));
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
      emit(state.copyWith(isSavingData: true));
      await _peopleManagementService.editEmployeeAddress(
          state.employee.id, state.company!.id, state.address);
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
}
