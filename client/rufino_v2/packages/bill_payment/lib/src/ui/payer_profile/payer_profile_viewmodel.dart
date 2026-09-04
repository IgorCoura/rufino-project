import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/payer_profile.dart';
import '../../domain/payer_profile_repository.dart';

/// Stage of the payer profile screen.
enum PayerProfileStatus {
  /// The cadastro is on its way.
  loading,

  /// No profile yet — the onboarding form takes the screen.
  onboarding,

  /// The profile is on screen.
  loaded,

  /// The profile could not be loaded.
  error,
}

/// Drives the payer profile screen — onboarding when absent, inline edits
/// once it exists.
class PayerProfileViewModel extends ChangeNotifier {
  /// Creates the view model.
  PayerProfileViewModel({required PayerProfileRepository repository})
      : _repository = repository;

  final PayerProfileRepository _repository;

  PayerProfile? _profile;
  PayerProfileStatus _status = PayerProfileStatus.loading;
  String? _errorMessage;
  bool _isMutating = false;

  /// The profile, once loaded.
  PayerProfile? get profile => _profile;

  /// The stage of the screen.
  PayerProfileStatus get status => _status;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Whether a mutation is in flight.
  bool get isMutating => _isMutating;

  /// Loads the profile; its absence is the onboarding state.
  Future<void> load() async {
    _status = PayerProfileStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final result = await _repository.getProfile();
    result.fold(
      onSuccess: (profile) {
        _profile = profile;
        _status = profile == null
            ? PayerProfileStatus.onboarding
            : PayerProfileStatus.loaded;
      },
      onError: (error, _) {
        _status = PayerProfileStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar o perfil do pagador.',
        );
      },
    );
    notifyListeners();
  }

  Future<bool> _mutate(
    Future<dynamic> Function() action, {
    required String fallback,
  }) async {
    _isMutating = true;
    _errorMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await action();
      (result as dynamic).fold(
        onSuccess: (_) => succeeded = true,
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(error, fallback: fallback);
        },
      );
    } finally {
      _isMutating = false;
      notifyListeners();
    }
    if (succeeded) await load();
    return succeeded;
  }

  /// Registers the profile — the onboarding submit.
  Future<bool> register({
    required String kind,
    required String legalName,
    required String primaryTaxId,
  }) =>
      _mutate(
        () => _repository.registerProfile(
          kind: kind,
          legalName: legalName,
          primaryTaxId: primaryTaxId,
        ),
        fallback: 'Não foi possível cadastrar o perfil.',
      );

  /// Renames the payer.
  Future<bool> saveLegalName(String legalName) => _mutate(
        () => _repository.changeLegalName(legalName),
        fallback: 'Não foi possível renomear.',
      );

  /// Adds an extra fiscal document.
  Future<bool> addTaxId(String taxId) => _mutate(
        () => _repository.addTaxId(taxId),
        fallback: 'Não foi possível adicionar o documento.',
      );

  /// Removes an extra fiscal document.
  Future<bool> removeTaxId(String taxId) => _mutate(
        () => _repository.removeTaxId(taxId),
        fallback: 'Não foi possível remover o documento.',
      );

  /// Turns CNPJ-root matching on or off.
  Future<bool> setCnpjRootMatching({required bool enabled}) => _mutate(
        () => _repository.setCnpjRootMatching(enabled: enabled),
        fallback: 'Não foi possível alterar o casamento por raiz.',
      );

  /// Sends the tenant's Asaas API key to be proven and stored server-side.
  Future<bool> linkAsaasAccount(String apiKey) => _mutate(
        () => _repository.linkAsaasAccount(apiKey),
        fallback: 'Não foi possível vincular a conta.',
      );

  /// Unlinks the account, removing the key from the server vault.
  Future<bool> unlinkAsaasAccount() => _mutate(
        () => _repository.unlinkAsaasAccount(),
        fallback: 'Não foi possível remover a chave.',
      );
}
