import 'dart:async';

import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:infinite_scroll_pagination/infinite_scroll_pagination.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/employee/domain/model/employee_with_role.dart';
import 'package:rufino/modules/employee/domain/model/role_info/department.dart';
import 'package:rufino/modules/employee/domain/model/role_info/position.dart';
import 'package:rufino/modules/employee/domain/model/role_info/role.dart';
import 'package:rufino/modules/employee/domain/model/search_params.dart';
import 'package:rufino/modules/employee/domain/model/status.dart';
import 'package:rufino/modules/employee/domain/model/workplace/workplace.dart';
import 'package:rufino/modules/employee/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'employees_list_event.dart';
part 'employees_list_state.dart';

class EmployeesListBloc extends Bloc<EmployeesListEvent, EmployeesListState> {
  final PeopleManagementService _peopleManagementService;
  final CompanyGlobalService _companyService;
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
    on<FeatchNextPage>(_onFeatchNextPage);
    on<RefreshPage>(_onRefreshPage);
    on<LoadInfoToCreateEmployee>(_onLoadInfoToCreateEmployee);
    on<ChangeDepartment>(_onChangeDepartment);
    on<ChangePosition>(_onChangePosition);
    on<ChangeRole>(_onChangeRole);
    on<ChangeWorkplace>(_onChangeWorkplace);
    on<LoadSingleEmployeeImage>(_onLoadSingleEmployeeImage);
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
      if (state.company != null &&
          state.nameNewEmployee != null &&
          state.role != null &&
          state.workplace != null) {
        await _peopleManagementService.createEmployee(state.company!.id,
            state.nameNewEmployee!, state.role!.id, state.workplace!.id);
        emit(state.copyWith(isLoading: false));
        add(RefreshPage());
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
    if (state.pagingState == null) {
      emit(state.copyWith(
          pagingState: PagingState<int, EmployeeWithRole>(
              isLoading: true, error: null)));
    } else {
      emit(state.copyWith(
          pagingState:
              state.pagingState!.copyWith(isLoading: true, error: null)));
    }

    try {
      var company = await _companyService.getSelectedCompany();
      var listStatus = await _peopleManagementService.getStatus(company.id);
      //var listStatus = <Status>[];
      listStatus.addAll(state.listStatus);

      emit(state.copyWith(
          listStatus: listStatus, isLoading: false, company: company));
      emit(state.copyWith(
          pagingState:
              state.pagingState!.copyWith(isLoading: false, error: null)));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          pagingState:
              state.pagingState!.copyWith(isLoading: false, error: null)));
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onChangeSortList(
      ChangeSortList event, Emitter<EmployeesListState> emit) async {
    var sort = state.isAscSort == false;
    emit(state.copyWith(isAscSort: sort, isLoading: true));
    try {
      add(RefreshPage());
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
      add(RefreshPage());
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
      add(RefreshPage());
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
    var employees = await _peopleManagementService.getEmployees(
        state.company!.id,
        state.searchParam == SearchParam.name ? searchInput : null,
        state.searchParam == SearchParam.role ? searchInput : null,
        state.selectedStatus == 0 ? null : state.selectedStatus,
        state.isAscSort ? 0 : 1,
        _pageSize,
        _sizeSkip);
    return employees;
  }

  // Future<List<EmployeeWithRole>> _fetchPage({int? pageKey}) async {
  //   _sizeSkip = pageKey ?? _sizeSkip;
  //   final newItems = await _searchEmpployee();
  //   final isLastPage = newItems.length < _pageSize;
  //   if (isLastPage) {
  //     pagingController.appendLastPage(newItems);
  //   } else {
  //     final nextPageKey = _sizeSkip + newItems.length;
  //     pagingController.appendPage(newItems, nextPageKey);
  //   }
  // }

  Future _onRefreshPage(
      RefreshPage event, Emitter<EmployeesListState> emit) async {
    emit(
      state.copyWith(
        pagingState: PagingState(),
      ),
    );
    _sizeSkip = 0;
    add(FeatchNextPage());
  }

  Future _onFeatchNextPage(
      FeatchNextPage event, Emitter<EmployeesListState> emit) async {
    if (state.pagingState == null) {
      return;
    }

    if (state.pagingState!.isLoading) return;

    emit(state.copyWith(
        pagingState:
            state.pagingState!.copyWith(isLoading: true, error: null)));

    try {
      final newItems = await _searchEmpployee();
      final isLastPage = newItems.length < _pageSize;
      final newKey = (state.pagingState!.keys?.last ?? 0) + _pageSize;
      _sizeSkip = newKey;

      // Adicionar funcionários imediatamente sem imagens
      emit(
        state.copyWith(
          pagingState: state.pagingState!.copyWith(
            isLoading: false,
            error: null,
            pages: [...?state.pagingState!.pages, newItems],
            keys: [...?state.pagingState!.keys, newKey],
            hasNextPage: !isLastPage,
          ),
        ),
      );

      // Disparar eventos assíncronos para carregar imagens de cada funcionário
      for (var employee in newItems) {
        add(LoadSingleEmployeeImage(employee.id));
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          exception: exception,
          pagingState: state.pagingState!.copyWith(isLoading: false)));
    }
  }

  Future _onLoadInfoToCreateEmployee(
      LoadInfoToCreateEmployee event, Emitter<EmployeesListState> emit) async {
    emit(state.copyWith(isLoadingInfoToCreateEmployee: true));
    var departments =
        await _peopleManagementService.getAllDepartment(state.company!.id);
    var workplaces =
        await _peopleManagementService.getAllWorkplace(state.company!.id);
    emit(state.copyWith(
        departments: departments,
        workplaces: workplaces,
        isLoadingInfoToCreateEmployee: false));
  }

  Future _onChangeDepartment(
      ChangeDepartment event, Emitter<EmployeesListState> emit) async {
    emit(state.copyWith(
        department: event.department, isLoadingInfoToCreateEmployee: true));

    try {
      List<Position> positions = [];
      if (event.department == const Department.empty()) {
        emit(state.copyWith(
          positions: positions,
          position: const Position.empty(),
          roles: [],
          role: const Role.empty(),
          isLoadingInfoToCreateEmployee: false,
        ));
        return;
      }
      positions = await _peopleManagementService.getAllPosition(
          event.department.id, state.company!.id);

      emit(state.copyWith(
        positions: positions,
        position: const Position.empty(),
        roles: [],
        role: const Role.empty(),
        isLoadingInfoToCreateEmployee: false,
      ));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoadingInfoToCreateEmployee: false, exception: exception));
    }
  }

  Future _onChangePosition(
      ChangePosition event, Emitter<EmployeesListState> emit) async {
    emit(state.copyWith(isLoadingInfoToCreateEmployee: false));

    try {
      List<Role> roles = [];
      if (event.position == const Position.empty()) {
        emit(state.copyWith(
          position: event.position,
          roles: roles,
          role: const Role.empty(),
          isLoadingInfoToCreateEmployee: false,
        ));
        return;
      }
      roles = await _peopleManagementService.getAllRole(
          event.position.id, state.company!.id);

      emit(state.copyWith(
        position: event.position,
        roles: roles,
        role: const Role.empty(),
        isLoadingInfoToCreateEmployee: false,
      ));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoadingInfoToCreateEmployee: false, exception: exception));
    }
  }

  void _onChangeRole(ChangeRole event, Emitter<EmployeesListState> emit) {
    emit(state.copyWith(role: event.role));
  }

  void _onChangeWorkplace(
      ChangeWorkplace event, Emitter<EmployeesListState> emit) {
    emit(state.copyWith(workplace: event.workplace));
  }

  Future _onLoadSingleEmployeeImage(
      LoadSingleEmployeeImage event, Emitter<EmployeesListState> emit) async {
    try {
      // Carregar imagem do servidor
      final imageBytes = await _peopleManagementService.getEmployeeImage(
        state.company!.id,
        event.employeeId,
      );

      if (imageBytes == null) {
        return; // Sem imagem para este funcionário
      }

      // Encontrar e atualizar o funcionário nas páginas
      if (state.pagingState?.pages != null) {
        final updatedPages = state.pagingState!.pages!.map((page) {
          return page.map((employee) {
            if (employee.id == event.employeeId) {
              return employee.copyWith(image: imageBytes);
            }
            return employee;
          }).toList();
        }).toList();

        emit(state.copyWith(
          pagingState: state.pagingState!.copyWith(
            pages: updatedPages,
          ),
        ));
      }
    } catch (ex, stacktrace) {
      // Ignora erros silenciosamente - funcionário ficará com imagem padrão
    }
  }
}
