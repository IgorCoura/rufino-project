part of 'employees_list_bloc.dart';

class EmployeesListState extends Equatable {
  final bool isAscSort;
  final List<Status> listStatus;
  final int selectedStatus;
  final SearchParam searchParam;
  final String? searchInput;
  final bool isLoading;
  final AplicationException? exception;
  final Company? company;
  final String? nameNewEmployee;
  final String textfieldErrorMessage;
  final PagingState<int, EmployeeWithRole>? pagingState;

  const EmployeesListState({
    this.listStatus = Status.defaultList,
    this.searchParam = SearchParam.name,
    this.searchInput,
    this.exception,
    this.company,
    this.nameNewEmployee,
    this.textfieldErrorMessage = "",
    this.selectedStatus = 0,
    this.isAscSort = true,
    this.isLoading = false,
    this.pagingState,
  });

  EmployeesListState copyWith(
      {bool? isAscSort,
      SearchParam? searchParam,
      int? selectedStatus,
      List<Status>? listStatus,
      String? searchInput,
      bool? isLoading,
      AplicationException? exception,
      Company? company,
      String? nameNewEmployee,
      String? textfieldErrorMessage,
      PagingState<int, EmployeeWithRole>? pagingState}) {
    return EmployeesListState(
        isAscSort: isAscSort ?? this.isAscSort,
        searchParam: searchParam ?? this.searchParam,
        listStatus: listStatus ?? this.listStatus,
        selectedStatus: selectedStatus ?? this.selectedStatus,
        searchInput: searchInput ?? this.searchInput,
        isLoading: isLoading ?? this.isLoading,
        exception: exception ?? this.exception,
        company: company ?? this.company,
        nameNewEmployee: nameNewEmployee ?? this.nameNewEmployee,
        textfieldErrorMessage:
            textfieldErrorMessage ?? this.textfieldErrorMessage,
        pagingState: pagingState ?? this.pagingState);
  }

  @override
  List<Object?> get props => [
        isAscSort,
        listStatus,
        selectedStatus,
        searchParam,
        searchInput,
        isLoading,
        exception,
        company,
        nameNewEmployee,
        textfieldErrorMessage,
        pagingState,
      ];
}
