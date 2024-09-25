import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:infinite_scroll_pagination/infinite_scroll_pagination.dart';
import 'package:rufino/modules/people/presentation/domain/model/employee.dart';
import 'package:rufino/modules/people/presentation/domain/model/search_params.dart';
import 'package:rufino/modules/people/presentation/domain/model/status.dart';
import 'package:rufino/modules/people/presentation/domain/services/people_management_service.dart';

part 'employees_list_event.dart';
part 'employees_list_state.dart';

class EmployeesListBloc extends Bloc<EmployeesListEvent, EmployeesListState> {
  final PeopleManagementService _peopleManagementService;
  final PagingController<int, Employee> pagingController =
      PagingController(firstPageKey: 0);
  int _pageSize = 15;
  int _sizeSkip = 0;

  EmployeesListBloc(this._peopleManagementService)
      : super(const EmployeesListState()) {
    on<InitialEmployeesListEvent>(_onInitialEmployeesListEvent);
    on<ChangeSortList>(_onChangeSortList);
    on<ChangeStatusSelect>(_onChangeStatusSelect);
    on<ChangeSearchParam>(_onChangeSearchParam);
    on<ChangeSearchInput>(_onChangeSearchInput);
    on<SearchEditComplet>(_onSearchEditComplet);
  }

  Future _onInitialEmployeesListEvent(
      InitialEmployeesListEvent event, Emitter<EmployeesListState> emit) async {
    emit(state.copyWith(isLoading: true));
    pagingController.addPageRequestListener((pageKey) {
      _fetchPage(pageKey: pageKey);
    });
    var listStatus = await _peopleManagementService.getStatus();
    listStatus.addAll(state.listStatus);
    await _fetchPage();
    pagingController.refresh();
    emit(state.copyWith(listStatus: listStatus, isLoading: false));
  }

  Future _onChangeSortList(
      ChangeSortList event, Emitter<EmployeesListState> emit) async {
    var sort = state.isAscSort == false;
    emit(state.copyWith(isAscSort: sort, isLoading: true));
    await _fetchPage();
    pagingController.refresh();
    emit(state.copyWith(isLoading: false));
  }

  Future _onChangeStatusSelect(
      ChangeStatusSelect event, Emitter<EmployeesListState> emit) async {
    emit(state.copyWith(selectedStatus: event.selection, isLoading: true));
    await _fetchPage();
    pagingController.refresh();
    emit(state.copyWith(isLoading: false));
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
    await _fetchPage();
    pagingController.refresh();
    emit(state.copyWith(isLoading: false));
  }

  Future<List<Employee>> _searchEmpployee() async {
    var searchInput = state.searchInput != null && state.searchInput!.isEmpty
        ? null
        : state.searchInput;
    var employees = await _peopleManagementService.getEmployees(
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
