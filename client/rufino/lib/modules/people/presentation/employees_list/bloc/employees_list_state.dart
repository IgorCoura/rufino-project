part of 'employees_list_bloc.dart';

class EmployeesListState extends Equatable {
  final bool isAscSort;
  final List<Status> listStatus;
  final int selectedStatus;
  final SearchParam searchParam;
  final String? searchInput;
  final bool isLoading;

  const EmployeesListState(
      {this.listStatus = Status.defaultList,
      this.searchParam = SearchParam.name,
      this.searchInput,
      this.selectedStatus = 0,
      this.isAscSort = true,
      this.isLoading = false});

  EmployeesListState copyWith(
      {bool? isAscSort,
      SearchParam? searchParam,
      int? selectedStatus,
      List<Status>? listStatus,
      String? searchInput,
      bool? isLoading}) {
    return EmployeesListState(
        isAscSort: isAscSort ?? this.isAscSort,
        searchParam: searchParam ?? this.searchParam,
        listStatus: listStatus ?? this.listStatus,
        selectedStatus: selectedStatus ?? this.selectedStatus,
        searchInput: searchInput ?? this.searchInput,
        isLoading: isLoading ?? this.isLoading);
  }

  @override
  List<Object?> get props => [
        isAscSort,
        listStatus,
        selectedStatus,
        searchParam,
        searchInput,
        isLoading
      ];
}
