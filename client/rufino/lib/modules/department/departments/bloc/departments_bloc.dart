import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/department/domain/models/department.dart';
import 'package:rufino/modules/department/domain/services/department_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'departments_event.dart';
part 'departments_state.dart';

class DepartmentsBloc extends Bloc<DepartmentsEvent, DepartmentsState> {
  final CompanyGlobalService _companyService;
  final DepartmentService _departmentService;
  DepartmentsBloc(this._companyService, this._departmentService)
      : super(DepartmentsState()) {
    on<LoadEvent>(_onLoadEvent);
  }

  Future _onLoadEvent(LoadEvent event, Emitter<DepartmentsState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));

      final company = await _companyService.getSelectedCompany();
      final department = await _departmentService.getAll(
        company.id,
      );

      emit(state.copyWith(
        isLoading: false,
        department: department,
      ));
    } catch (ex, stacktrace) {
      var exception = _departmentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }
}
