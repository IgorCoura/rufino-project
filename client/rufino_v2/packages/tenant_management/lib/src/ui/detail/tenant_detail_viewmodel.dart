import 'package:flutter/foundation.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/tenant.dart';
import '../../domain/tenant_enums.dart';
import '../../domain/tenant_exception.dart';
import '../../domain/tenant_repository.dart';

/// Stage of the detail screen as a whole.
enum TenantDetailStatus {
  /// Loading the cadastro.
  loading,

  /// The cadastro is on screen.
  loaded,

  /// It could not be loaded.
  error,
}

/// Stage of one editable block.
///
/// There is no `loading` here, unlike the employee profile this pattern comes
/// from: `GET /tenants/{id}` brings the whole cadastro at once, so a block is
/// never waiting for its own data — only for its own save.
enum TenantSectionStatus {
  /// Showing data.
  idle,

  /// A save is in flight.
  saving,
}

/// Drives the full cadastro of one tenant.
///
/// Each block saves on its own: one block, one endpoint, one idempotency key.
/// A block that fails keeps its own error and leaves the rest of the screen
/// untouched — which is the whole reason the blocks are separate.
class TenantDetailViewModel extends ChangeNotifier {
  /// Creates the view model for [tenantId].
  TenantDetailViewModel({
    required TenantRepository repository,
    required this.tenantId,
    CepLookupService? cepService,
  })  : _repository = repository,
        _cepService = cepService;

  final TenantRepository _repository;
  final CepLookupService? _cepService;

  /// The tenant being shown.
  final String tenantId;

  Tenant? _tenant;
  TenantDetailStatus _status = TenantDetailStatus.loading;
  String? _errorMessage;
  String? _snackMessage;

  TenantSectionStatus _identificationStatus = TenantSectionStatus.idle;
  TenantSectionStatus _contactStatus = TenantSectionStatus.idle;
  TenantSectionStatus _addressStatus = TenantSectionStatus.idle;
  TenantSectionStatus _accessStatus = TenantSectionStatus.idle;
  TenantSectionStatus _productsStatus = TenantSectionStatus.idle;

  String? _identificationError;
  String? _contactError;
  String? _addressError;
  String? _accessError;

  /// The cadastro, once loaded.
  Tenant? get tenant => _tenant;

  /// The stage of the screen.
  TenantDetailStatus get status => _status;

  /// The message to show when the cadastro could not be loaded.
  String? get errorMessage => _errorMessage;

  /// Status of the identification block.
  TenantSectionStatus get identificationStatus => _identificationStatus;

  /// Status of the contact block.
  TenantSectionStatus get contactStatus => _contactStatus;

  /// Status of the address block.
  TenantSectionStatus get addressStatus => _addressStatus;

  /// Status of the access tab.
  TenantSectionStatus get accessStatus => _accessStatus;

  /// Status of the products tab.
  TenantSectionStatus get productsStatus => _productsStatus;

  /// Error of the identification block, if its last save failed.
  String? get identificationError => _identificationError;

  /// Error of the contact block, if its last save failed.
  String? get contactError => _contactError;

  /// Error of the address block, if its last save failed.
  String? get addressError => _addressError;

  /// Error of the access tab, if its last action failed.
  String? get accessError => _accessError;

  /// Whether a suspended cadastro is blocking every change.
  bool get isFrozen => _tenant?.isSuspended ?? false;

  /// Takes the pending success message, if any, clearing it.
  String? consumeSnackMessage() {
    final message = _snackMessage;
    _snackMessage = null;
    return message;
  }

  /// Loads the cadastro.
  Future<void> load() async {
    _status = TenantDetailStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final result = await _repository.getTenant(tenantId);
    result.fold(
      onSuccess: (tenant) {
        _tenant = tenant;
        _status = TenantDetailStatus.loaded;
      },
      onError: (error, _) {
        _status = TenantDetailStatus.error;
        _errorMessage = tenantErrorMessage(
          error,
          fallback: 'Não foi possível carregar o cliente.',
        );
      },
    );
    notifyListeners();
  }

  /// Looks a CEP up, returning `null` when there is no lookup available or
  /// the code does not exist.
  Future<CepLookup?> lookupCep(String cep) async {
    final service = _cepService;
    if (service == null) return null;
    try {
      return await service.lookup(cep);
    } on CepException {
      return null;
    }
  }

  /// Renames the tenant.
  Future<bool> saveIdentification({
    required String legalName,
    required String tradeName,
  }) {
    return _runSectionSave(
      set: (status) => _identificationStatus = status,
      setError: (message) => _identificationError = message,
      success: 'Identificação atualizada.',
      fallback: 'Não foi possível salvar a identificação.',
      action: () => _repository.editDetails(
        tenantId,
        legalName: legalName,
        tradeName: tradeName,
      ),
    );
  }

  /// Replaces the contact channel.
  Future<bool> saveContact({required String email, required String phone}) {
    return _runSectionSave(
      set: (status) => _contactStatus = status,
      setError: (message) => _contactError = message,
      success: 'Contato atualizado.',
      fallback: 'Não foi possível salvar o contato.',
      action: () =>
          _repository.changeContact(tenantId, email: email, phone: phone),
    );
  }

  /// Replaces the address.
  Future<bool> saveAddress(TenantAddress address) {
    return _runSectionSave(
      set: (status) => _addressStatus = status,
      setError: (message) => _addressError = message,
      success: 'Endereço atualizado.',
      fallback: 'Não foi possível salvar o endereço.',
      action: () => _repository.changeAddress(tenantId, address),
    );
  }

  /// Grants somebody access to this tenant.
  Future<bool> grantMembership({
    required String email,
    required String role,
  }) {
    return _runSectionSave(
      set: (status) => _accessStatus = status,
      setError: (message) => _accessError = message,
      success: 'Acesso concedido. O convite vai para $email.',
      fallback: 'Não foi possível conceder o acesso.',
      action: () =>
          _repository.grantMembership(tenantId, email: email, role: role),
    );
  }

  /// Revokes somebody's access.
  Future<bool> revokeMembership(String email) {
    return _runSectionSave(
      set: (status) => _accessStatus = status,
      setError: (message) => _accessError = message,
      success: 'Acesso revogado.',
      fallback: 'Não foi possível revogar o acesso.',
      action: () => _repository.revokeMembership(tenantId, email),
    );
  }

  /// Re-sends to the identity provider whatever never arrived.
  Future<bool> reprovisionAccess() {
    return _runSectionSave(
      set: (status) => _accessStatus = status,
      setError: (message) => _accessError = message,
      success: 'Acessos reenviados ao provedor de identidade.',
      fallback: 'Não foi possível reenviar os acessos.',
      action: () => _repository.reprovisionAccess(tenantId),
    );
  }

  /// Turns [product] on or off for this tenant.
  Future<bool> setProduct(String product, {required bool enabled}) {
    return _runSectionSave(
      set: (status) => _productsStatus = status,
      setError: (_) {},
      success: enabled
          ? '${TenantProductLabels.label(product)} habilitado.'
          : '${TenantProductLabels.label(product)} desabilitado.',
      fallback: 'Não foi possível alterar o produto.',
      action: () => enabled
          ? _repository.activateProduct(tenantId, product)
          : _repository.deactivateProduct(tenantId, product),
    );
  }

  /// Freezes the cadastro.
  Future<bool> suspend(String reason) {
    return _runSectionSave(
      set: (status) => _identificationStatus = status,
      setError: (message) => _identificationError = message,
      success: 'Cliente suspenso.',
      fallback: 'Não foi possível suspender o cliente.',
      action: () => _repository.suspend(tenantId, reason),
    );
  }

  /// Lifts a suspension.
  Future<bool> reactivate() {
    return _runSectionSave(
      set: (status) => _identificationStatus = status,
      setError: (message) => _identificationError = message,
      success: 'Cliente reativado.',
      fallback: 'Não foi possível reativar o cliente.',
      action: () => _repository.reactivate(tenantId),
    );
  }

  /// Runs one write, reloading the cadastro when it succeeds.
  ///
  /// The reload is what keeps derived state honest: the access provisioning
  /// of the tenant is computed from its grants, so a grant that changed makes
  /// the banner on top of the screen stale until the aggregate comes back.
  Future<bool> _runSectionSave({
    required void Function(TenantSectionStatus) set,
    required void Function(String?) setError,
    required String success,
    required String fallback,
    required Future<Result<void>> Function() action,
  }) async {
    set(TenantSectionStatus.saving);
    setError(null);
    notifyListeners();

    final result = await action();
    var saved = false;

    await result.fold(
      onSuccess: (_) async {
        saved = true;
        _snackMessage = success;
      },
      onError: (error, _) async {
        setError(tenantErrorMessage(error, fallback: fallback));
      },
    );

    set(TenantSectionStatus.idle);

    if (saved) {
      // Recarrega antes de notificar: uma tela que volta a ler antes do
      // agregado chegar mostraria o estado velho por um frame.
      final reloaded = await _repository.getTenant(tenantId);
      final tenant = reloaded.valueOrNull;
      if (tenant != null) _tenant = tenant;
    }

    notifyListeners();
    return saved;
  }
}
