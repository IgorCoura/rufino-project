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
  final bool isLoadingInfoToCreateEmployee;
  final List<Department> departments;
  final List<Position> positions;
  final List<Role> roles;
  final Department department;
  final Position position;
  final Role role;
  final Workplace workplace;
  final List<Workplace> workplaces;

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
    this.isLoadingInfoToCreateEmployee = false,
    this.departments = const [],
    this.positions = const [],
    this.roles = const [],
    this.department = const Department.empty(),
    this.position = const Position.empty(),
    this.role = const Role.empty(),
    this.workplace = const Workplace.empty(),
    this.workplaces = const [],
  });

  EmployeesListState copyWith({
    bool? isAscSort,
    SearchParam? searchParam,
    int? selectedStatus,
    List<Status>? listStatus,
    String? searchInput,
    bool? isLoading,
    AplicationException? exception,
    Company? company,
    String? nameNewEmployee,
    String? textfieldErrorMessage,
    PagingState<int, EmployeeWithRole>? pagingState,
    bool? isLoadingInfoToCreateEmployee,
    List<Department>? departments,
    List<Position>? positions,
    List<Role>? roles,
    Department? department,
    Position? position,
    Role? role,
    Workplace? workplace,
    List<Workplace>? workplaces,    Map<String, List<int>>? employeeImages,  }) {
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
        pagingState: pagingState ?? this.pagingState,
        isLoadingInfoToCreateEmployee:
            isLoadingInfoToCreateEmployee ?? this.isLoadingInfoToCreateEmployee,
        departments: departments ?? this.departments,
        positions: positions ?? this.positions,
        roles: roles ?? this.roles,
        department: department ?? this.department,
        position: position ?? this.position,
        role: role ?? this.role,
        workplace: workplace ?? this.workplace,
        workplaces: workplaces ?? this.workplaces);
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
        isLoadingInfoToCreateEmployee,
        departments.hashCode,
        positions.hashCode,
        roles.hashCode,
        department,
        position,
        role,
        workplace,
        workplaces.hashCode,
      ];
}
