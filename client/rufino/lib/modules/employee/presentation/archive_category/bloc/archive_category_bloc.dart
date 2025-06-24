import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/archive_category.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/description.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/event.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/listen_events.dart';
import 'package:rufino/modules/employee/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'archive_category_event.dart';
part 'archive_category_state.dart';

class ArchiveCategoryBloc
    extends Bloc<ArchiveCategoryEvent, ArchiveCategoryState> {
  final PeopleManagementService _peopleManagementService;
  final CompanyGlobalService _companyService;

  ArchiveCategoryBloc(this._peopleManagementService, this._companyService)
      : super(ArchiveCategoryState()) {
    on<InitialEvent>(_onInitialEvent);
    on<SaveEvent>(_onSaveEvent);
    on<SnackMessageWasShow>(_onSnackMessageWasShow);
    on<CreateNewArchiveCategoryEvent>(_onCreateNewArchiveCategoryEvent);
  }

  Future _onInitialEvent(
      InitialEvent event, Emitter<ArchiveCategoryState> emit) async {
    emit(state.copyWith(isLoading: true));

    try {
      final company = await _companyService.getSelectedCompany();
      final categories =
          await _peopleManagementService.getArchiveCategories(company.id);
      final events =
          await _peopleManagementService.getArchiveEvents(company.id);

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

  Future _onSaveEvent(
      SaveEvent event, Emitter<ArchiveCategoryState> emit) async {
    emit(state.copyWith(isSavingData: true));

    try {
      final company = await _companyService.getSelectedCompany();

      var currentCategory = event.category;

      for (var change in event.changes) {
        if (change is ListenEvents) {
          var newListenEvents = change;
          final addEvents = newListenEvents.list.where((newEvent) {
            return !event.category.listenEvents.list
                .any((existingEvent) => existingEvent.id == newEvent.id);
          }).toList();

          final addEventsIds = addEvents.map((e) => int.parse(e.id)).toList();

          final removeEvents =
              event.category.listenEvents.list.where((existingEvent) {
            return !newListenEvents.list
                .any((newEvents) => newEvents.id == existingEvent.id);
          }).toList();

          final removeEventsIds =
              removeEvents.map((e) => int.parse(e.id)).toList();

          await _peopleManagementService.addArchiveCategoryEvent(
              company.id, event.category.id, addEventsIds);
          await _peopleManagementService.removeArchiveCategoryEvent(
              company.id, event.category.id, removeEventsIds);

          currentCategory =
              currentCategory.copyWith(listenEvents: newListenEvents);
        }
        if (change is Description) {
          var newDescription = change;
          await _peopleManagementService.editDescriptionArchiveCategory(
              company.id, event.category.id, newDescription.value);
          currentCategory =
              currentCategory.copyWith(description: newDescription);
        }
      }

      var categoryIndex = state.archiveCategories
          .indexWhere((element) => element.id == currentCategory.id);
      var newCategories = List<ArchiveCategory>.from(state.archiveCategories);
      newCategories[categoryIndex] = currentCategory;

      emit(state.copyWith(
        archiveCategories: newCategories,
        isSavingData: false,
        snackMessage: "Informações alterado com sucesso.",
      ));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  void _onSnackMessageWasShow(
      SnackMessageWasShow event, Emitter<ArchiveCategoryState> emit) {
    emit(state.copyWith(snackMessage: ""));
  }

  Future _onCreateNewArchiveCategoryEvent(CreateNewArchiveCategoryEvent event,
      Emitter<ArchiveCategoryState> emit) async {
    emit(state.copyWith(isLoading: true));

    try {
      final company = await _companyService.getSelectedCompany();
      await _peopleManagementService.createArchiveCategory(
          company.id, event.category);
      final categories =
          await _peopleManagementService.getArchiveCategories(company.id);
      emit(state.copyWith(archiveCategories: categories, isSavingData: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }
}
