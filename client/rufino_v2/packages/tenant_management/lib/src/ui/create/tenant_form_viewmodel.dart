import 'package:flutter/foundation.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../data/tenant_api_models.dart';
import '../../domain/tenant_enums.dart';
import '../../domain/tenant_exception.dart';
import '../../domain/tenant_repository.dart';

/// Stage of the cadastro form.
enum TenantFormStatus {
  /// Waiting for the user.
  idle,

  /// Sending.
  saving,
}

/// Drives the cadastro of a new tenant.
///
/// The kind of customer is state, not a field: choosing it reshapes the form
/// — an individual has no trade name and a CPF, a company has both a trade
/// name and a CNPJ — so the screen cannot even offer what the domain would
/// refuse.
class TenantFormViewModel extends ChangeNotifier {
  /// Creates the view model.
  TenantFormViewModel({
    required TenantRepository repository,
    CepLookupService? cepService,
  })  : _repository = repository,
        _cepService = cepService;

  final TenantRepository _repository;
  final CepLookupService? _cepService;

  String _kind = TenantKinds.company;
  TenantFormStatus _status = TenantFormStatus.idle;
  String? _errorMessage;
  final Set<String> _products = {};

  /// `Individual` or `Company`.
  String get kind => _kind;

  /// Whether the customer being registered is a natural person.
  bool get isIndividual => _kind == TenantKinds.individual;

  /// The stage of the form.
  TenantFormStatus get status => _status;

  /// Whether a submission is in flight.
  bool get isSaving => _status == TenantFormStatus.saving;

  /// The message of the last failed submission.
  String? get errorMessage => _errorMessage;

  /// The products picked to be enabled right away.
  Set<String> get products => Set.unmodifiable(_products);

  /// Switches the kind of customer being registered.
  void setKind(String kind) {
    if (_kind == kind) return;
    _kind = kind;
    notifyListeners();
  }

  /// Adds or removes [product] from the initial set.
  void toggleProduct(String product) {
    if (!_products.remove(product)) _products.add(product);
    notifyListeners();
  }

  /// Looks a CEP up, returning `null` when unavailable or unknown.
  Future<CepLookup?> lookupCep(String cep) async {
    final service = _cepService;
    if (service == null) return null;
    try {
      return await service.lookup(cep);
    } on CepException {
      return null;
    }
  }

  /// Registers the tenant, returning its new id — or `null` when refused.
  Future<String?> submit(RegisterTenantInput input) async {
    _status = TenantFormStatus.saving;
    _errorMessage = null;
    notifyListeners();

    final result = await _repository.registerTenant(input);
    String? id;

    result.fold(
      onSuccess: (newId) => id = newId,
      onError: (error, _) {
        _errorMessage = tenantErrorMessage(
          error,
          fallback: 'Não foi possível cadastrar o cliente.',
        );
      },
    );

    _status = TenantFormStatus.idle;
    notifyListeners();
    return id;
  }
}
