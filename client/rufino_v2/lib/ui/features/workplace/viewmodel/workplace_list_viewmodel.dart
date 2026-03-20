import 'package:flutter/foundation.dart';

import '../../../../domain/entities/workplace.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/workplace_repository.dart';

/// Possible statuses for the workplace list screen.
enum WorkplaceListStatus { loading, idle, error }

/// Loads and exposes the list of workplaces for the currently selected company.
///
/// Depends on [CompanyRepository] to resolve the active company id and on
/// [WorkplaceRepository] to fetch the workplaces.
class WorkplaceListViewModel extends ChangeNotifier {
  WorkplaceListViewModel({
    required CompanyRepository companyRepository,
    required WorkplaceRepository workplaceRepository,
  })  : _companyRepository = companyRepository,
        _workplaceRepository = workplaceRepository;

  final CompanyRepository _companyRepository;
  final WorkplaceRepository _workplaceRepository;

  List<Workplace> _workplaces = [];
  WorkplaceListStatus _status = WorkplaceListStatus.idle;
  String? _errorMessage;

  /// The workplaces loaded from the API, empty while loading or on error.
  List<Workplace> get workplaces => _workplaces;

  /// Whether the list is currently being fetched.
  bool get isLoading => _status == WorkplaceListStatus.loading;

  /// Whether the last fetch resulted in an error.
  bool get hasError => _status == WorkplaceListStatus.error;

  /// Human-readable error message set when [hasError] is true.
  String? get errorMessage => _errorMessage;

  /// Fetches and caches the workplace list for the currently selected company.
  Future<void> loadWorkplaces() async {
    _status = WorkplaceListStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final companyResult = await _companyRepository.getSelectedCompany();
    final companyId = companyResult.valueOrNull?.id;

    if (companyId == null) {
      _status = WorkplaceListStatus.error;
      _errorMessage = 'Nenhuma empresa selecionada.';
      notifyListeners();
      return;
    }

    final result = await _workplaceRepository.getWorkplaces(companyId);
    result.fold(
      onSuccess: (data) {
        _workplaces = data;
        _status = WorkplaceListStatus.idle;
      },
      onError: (_) {
        _workplaces = [];
        _status = WorkplaceListStatus.error;
        _errorMessage = 'Falha ao carregar locais de trabalho.';
      },
    );
    notifyListeners();
  }
}
