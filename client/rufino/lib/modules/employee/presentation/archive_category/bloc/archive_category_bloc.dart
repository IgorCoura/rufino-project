import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_service.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/archive_category.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/event.dart';
import 'package:rufino/modules/employee/domain/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'archive_category_event.dart';
part 'archive_category_state.dart';

class ArchiveCategoryBloc
    extends Bloc<ArchiveCategoryEvent, ArchiveCategoryState> {
  final PeopleManagementService _peopleManagementService;
  final CompanyService _companyService;

  ArchiveCategoryBloc(this._peopleManagementService, this._companyService)
      : super(ArchiveCategoryState()) {
    on<InitialEvent>(_onInitialEvent);
  }

  Future _onInitialEvent(
      InitialEvent event, Emitter<ArchiveCategoryState> emit) async {
    emit(state.copyWith(isLoading: true));

    try {
      final company = await _companyService.getSelectedCompany();
      final categories =
          await _peopleManagementService.getArchiveCategories(company.id);
      final events = await _peopleManagementService.getEvents(company.id);

      emit(state.copyWith(
        isLoading: false,
        archiveCategories: categories,
        events: events,
      ));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }
}
