import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:infinite_scroll_pagination/infinite_scroll_pagination.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/domain/services/company_service.dart';
import 'package:rufino/modules/employee/domain/model/employee_with_role.dart';
import 'package:rufino/modules/employee/domain/model/search_params.dart';
import 'package:rufino/modules/employee/domain/model/status.dart';
import 'package:rufino/modules/employee/domain/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'employees_list_event.dart';
part 'employees_list_state.dart';

class EmployeesListBloc extends Bloc<EmployeesListEvent, EmployeesListState> {
  final PeopleManagementService _peopleManagementService;
  final CompanyService _companyService;
  final PagingController<int, EmployeeWithRole> pagingController =
      PagingController(firstPageKey: 0);
  final int _pageSize = 15;
  int _sizeSkip = 0;

  EmployeesListBloc(this._peopleManagementService, this._companyService)
      : super(const EmployeesListState()) {
    on<InitialEmployeesListEvent>(_onInitialEmployeesListEvent);
    on<ChangeSortList>(_onChangeSortList);
    on<ChangeStatusSelect>(_onChangeStatusSelect);
    on<ChangeSearchParam>(_onChangeSearchParam);
    on<ChangeSearchInput>(_onChangeSearchInput);
    on<SearchEditComplet>(_onSearchEditComplet);
    on<ChangeNameNewEmployee>(_onChangeNameNewEmployee);
    on<CreateNewEmployee>(_onCreateEmployee);
    on<ErrorEvent>(_onErrorEvent);
  }

  void _onErrorEvent(ErrorEvent event, Emitter<EmployeesListState> emit) {
    emit(state.copyWith(exception: event.exception, isLoading: false));
  }

  void _onChangeNameNewEmployee(
      ChangeNameNewEmployee event, Emitter<EmployeesListState> emit) {
    emit(state.copyWith(textfieldErrorMessage: ""));
    final regex = RegExp(r"^[a-zA-ZÀ-ÿ']+(?: [a-zA-ZÀ-ÿ']+)+$");
    if (!regex.hasMatch(event.name)) {
      emit(state.copyWith(textfieldErrorMessage: "Nome inválido"));
      return;
    }
    emit(
        state.copyWith(nameNewEmployee: event.name, textfieldErrorMessage: ""));
  }

  Future _onCreateEmployee(
      CreateNewEmployee event, Emitter<EmployeesListState> emit) async {
    emit(state.copyWith(isLoading: true));
    try {
      if (state.company != null && state.nameNewEmployee != null) {
        await _peopleManagementService.createEmployee(
            state.company!.id, state.nameNewEmployee!);
        emit(state.copyWith(isLoading: false));
      } else {
        throw AplicationErrors.emplyee.errorTryCreateEmployee;
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onInitialEmployeesListEvent(
      InitialEmployeesListEvent event, Emitter<EmployeesListState> emit) async {
    emit(const EmployeesListState(isLoading: true));
    try {
      pagingController.addPageRequestListener((pageKey) async {
        try {
          await _fetchPage(pageKey: pageKey);
        } catch (ex, stacktrace) {
          var exception = _peopleManagementService.treatErrors(ex, stacktrace);
          add(ErrorEvent(exception));
        }
      });
      var company = await _companyService.getSelectedCompany();
      var listStatus = await _peopleManagementService.getStatus(company.id);
      listStatus.addAll(state.listStatus);
      await _fetchPage();
      pagingController.refresh();
      emit(state.copyWith(
          listStatus: listStatus, isLoading: false, company: company));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onChangeSortList(
      ChangeSortList event, Emitter<EmployeesListState> emit) async {
    var sort = state.isAscSort == false;
    emit(state.copyWith(isAscSort: sort, isLoading: true));
    try {
      await _fetchPage();
      pagingController.refresh();
      emit(state.copyWith(isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onChangeStatusSelect(
      ChangeStatusSelect event, Emitter<EmployeesListState> emit) async {
    emit(state.copyWith(selectedStatus: event.selection, isLoading: true));
    try {
      await _fetchPage();
      pagingController.refresh();
      emit(state.copyWith(isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onChangeSearchParam(
      ChangeSearchParam event, Emitter<EmployeesListState> emit) {
    emit(
      state.copyWith(
          searchParam:
              SearchParam.fromId(event.paramSelected ?? SearchParam.name.id)),
    );
  }

  void _onChangeSearchInput(
      ChangeSearchInput event, Emitter<EmployeesListState> emit) {
    emit(state.copyWith(searchInput: event.input));
  }

  Future _onSearchEditComplet(
      SearchEditComplet event, Emitter<EmployeesListState> emit) async {
    emit(state.copyWith(isLoading: true));
    try {
      await _fetchPage();
      pagingController.refresh();
      emit(state.copyWith(isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future<List<EmployeeWithRole>> _searchEmpployee() async {
    if (state.company == null) {
      return List.empty();
    }
    var searchInput = state.searchInput != null && state.searchInput!.isEmpty
        ? null
        : state.searchInput;
    var employees = await _peopleManagementService.getEmployeesWithRoles(
        state.company!.id,
        state.searchParam == SearchParam.name ? searchInput : null,
        state.searchParam == SearchParam.role ? searchInput : null,
        state.selectedStatus == 0 ? null : state.selectedStatus,
        state.isAscSort ? 0 : 1,
        _pageSize,
        _sizeSkip);
    return employees;
  }

  Future<void> _fetchPage({int? pageKey}) async {
    _sizeSkip = pageKey ?? _sizeSkip;
    final newItems = await _searchEmpployee();
    final isLastPage = newItems.length < _pageSize;
    if (isLastPage) {
      pagingController.appendLastPage(newItems);
    } else {
      final nextPageKey = _sizeSkip + newItems.length;
      pagingController.appendPage(newItems, nextPageKey);
    }
  }
}