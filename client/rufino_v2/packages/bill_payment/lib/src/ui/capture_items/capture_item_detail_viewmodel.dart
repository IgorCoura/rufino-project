import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/capture_item.dart';
import '../../domain/capture_item_repository.dart';

/// Stage of the quarantine item detail.
enum CaptureItemDetailStatus {
  /// The item is on its way.
  loading,

  /// The item is on screen.
  loaded,

  /// The item could not be loaded.
  error,
}

/// Drives the quarantine item detail and its two actions.
class CaptureItemDetailViewModel extends ChangeNotifier {
  /// Creates the view model for [itemId].
  CaptureItemDetailViewModel({
    required CaptureItemRepository repository,
    required this.itemId,
  }) : _repository = repository;

  final CaptureItemRepository _repository;

  /// The item being shown.
  final String itemId;

  CaptureItem? _item;
  CaptureItemDetailStatus _status = CaptureItemDetailStatus.loading;
  String? _errorMessage;
  String? _infoMessage;
  bool _isMutating = false;
  String? _claimedBillId;

  /// The item, once loaded.
  CaptureItem? get item => _item;

  /// The stage of the detail.
  CaptureItemDetailStatus get status => _status;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// The outcome message of the last action, for a snackbar.
  String? get infoMessage => _infoMessage;

  /// Whether an action is in flight.
  bool get isMutating => _isMutating;

  /// The bill created by a successful claim — the screen navigates to it.
  String? get claimedBillId => _claimedBillId;

  /// Loads the item.
  Future<void> load() async {
    _status = CaptureItemDetailStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final result = await _repository.getItem(itemId);
    result.fold(
      onSuccess: (item) {
        _item = item;
        _status = CaptureItemDetailStatus.loaded;
      },
      onError: (error, _) {
        _status = CaptureItemDetailStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar o item.',
        );
      },
    );
    notifyListeners();
  }

  /// Claims the item as this tenant's bill.
  Future<bool> claim() async {
    _isMutating = true;
    _errorMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await _repository.claimItem(itemId);
      result.fold(
        onSuccess: (outcome) {
          succeeded = true;
          _claimedBillId = outcome.billId;
        },
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível reivindicar o item.',
          );
        },
      );
    } finally {
      _isMutating = false;
      notifyListeners();
    }
    return succeeded;
  }

  /// Sends the item back through the extraction cascade.
  Future<bool> reprocess() async {
    _isMutating = true;
    _errorMessage = null;
    _infoMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await _repository.reprocessItem(itemId);
      result.fold(
        onSuccess: (_) {
          succeeded = true;
          _infoMessage = 'Item devolvido à fila — o processamento roda em '
              'segundo plano.';
        },
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível reprocessar o item.',
          );
        },
      );
    } finally {
      _isMutating = false;
      notifyListeners();
    }
    if (succeeded) await load();
    return succeeded;
  }

  /// Dismisses the item: the user does not recognise the charge.
  ///
  /// Reversible by [reprocess] — the message says so, because a dismissal that
  /// felt final would make people hesitate, and a queue nobody empties is the
  /// problem this action exists to solve.
  Future<bool> dismiss({String? note}) async {
    _isMutating = true;
    _errorMessage = null;
    _infoMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await _repository.dismissItem(itemId, note: note);
      result.fold(
        onSuccess: (_) {
          succeeded = true;
          _infoMessage = 'Item marcado como não reconhecido. Ele saiu da lista '
              'de pendências — dá para reabrir a qualquer momento.';
        },
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível reprovar o item.',
          );
        },
      );
    } finally {
      _isMutating = false;
      notifyListeners();
    }
    if (succeeded) await load();
    return succeeded;
  }

  /// Uploads the bill the user fetched by hand and returns the item to the queue.
  Future<bool> attachArtifact(
    List<int> bytes, {
    required String fileName,
    required String contentType,
  }) async {
    _isMutating = true;
    _errorMessage = null;
    _infoMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await _repository.attachArtifact(
        itemId,
        bytes,
        fileName: fileName,
        contentType: contentType,
      );
      result.fold(
        onSuccess: (_) {
          succeeded = true;
          _infoMessage = 'Boleto anexado. O item voltou para a fila e será lido '
              'em segundo plano.';
        },
        onError: (error, _) {
          _errorMessage = billPaymentErrorMessage(
            error,
            fallback: 'Não foi possível anexar o boleto.',
          );
        },
      );
    } finally {
      _isMutating = false;
      notifyListeners();
    }
    if (succeeded) await load();
    return succeeded;
  }
}
