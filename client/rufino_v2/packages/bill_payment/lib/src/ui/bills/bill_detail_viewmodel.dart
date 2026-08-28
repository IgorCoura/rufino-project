import 'package:flutter/foundation.dart';

import '../../domain/bill_detail.dart';
import '../../domain/bill_payment_exception.dart';
import '../../domain/bill_repository.dart';

/// Stage of the bill detail.
enum BillDetailStatus {
  /// The bill is on its way.
  loading,

  /// The bill is on screen.
  loaded,

  /// The bill could not be loaded.
  error,
}

/// Drives the approval screen: the twelve checks and the three decisions.
class BillDetailViewModel extends ChangeNotifier {
  /// Creates the view model for [billId].
  ///
  /// [clock] exists for tests — the snapshot-age rule compares against
  /// "now".
  BillDetailViewModel({
    required BillRepository repository,
    required this.billId,
    DateTime Function()? clock,
  })  : _repository = repository,
        _clock = clock ?? DateTime.now;

  final BillRepository _repository;
  final DateTime Function() _clock;

  /// The bill being shown.
  final String billId;

  BillDetail? _bill;
  BillDetailStatus _status = BillDetailStatus.loading;
  String? _errorMessage;
  String? _infoMessage;
  bool _isMutating = false;

  /// The bill, once loaded.
  BillDetail? get bill => _bill;

  /// The stage of the detail.
  BillDetailStatus get status => _status;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// The outcome message of the last action, for a snackbar.
  String? get infoMessage => _infoMessage;

  /// Whether an action is in flight.
  bool get isMutating => _isMutating;

  /// Whether the lookup snapshot is too old to sustain an approval.
  bool get isSnapshotStale => _bill?.isSnapshotStaleAt(_clock()) ?? true;

  /// Whether the approve button can be enabled right now.
  ///
  /// Status and snapshot age together — a stale snapshot gets "revalide
  /// antes de aprovar" instead of a click that bounces on a 409.
  bool get canApprove => _bill?.canApproveAt(_clock()) ?? false;

  /// The earliest schedule date selectable today.
  DateTime get earliestScheduleDate =>
      _bill?.earliestScheduleDate(_clock()) ?? _clock();

  /// Loads the bill.
  Future<void> load() async {
    _status = BillDetailStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final result = await _repository.getBillDetail(billId);
    result.fold(
      onSuccess: (bill) {
        _bill = bill;
        _status = BillDetailStatus.loaded;
      },
      onError: (error, _) {
        _status = BillDetailStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar o boleto.',
        );
      },
    );
    notifyListeners();
  }

  Future<bool> _act(
    Future<dynamic> Function() action, {
    required String fallback,
    String? info,
  }) async {
    _isMutating = true;
    _errorMessage = null;
    _infoMessage = null;
    notifyListeners();

    var succeeded = false;
    try {
      final result = await action();
      (result as dynamic).fold(
        onSuccess: (_) {
          succeeded = true;
          _infoMessage = info;
        },
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

  /// Re-runs the official lookup and the twelve checks.
  Future<bool> revalidate() => _act(
        () => _repository.revalidateBill(billId),
        fallback: 'Não foi possível revalidar.',
        info: 'Verificações reexecutadas.',
      );

  /// Authorizes the payment for [scheduleFor].
  ///
  /// [acknowledgeRisk] carries the explicit acceptance a Danger bill
  /// requires (ADR-015).
  Future<bool> approve({
    required DateTime scheduleFor,
    String? note,
    bool acknowledgeRisk = false,
  }) =>
      _act(
        () => _repository.approveBill(
          billId,
          scheduleFor: scheduleFor,
          note: note,
          acknowledgeRisk: acknowledgeRisk,
        ),
        fallback: 'Não foi possível aprovar.',
        info: 'Pagamento autorizado.',
      );

  /// Refuses the bill with a mandatory [reason].
  Future<bool> deny(String reason) => _act(
        () => _repository.denyBill(billId, reason),
        fallback: 'Não foi possível negar.',
        info: 'Boleto negado.',
      );

  /// Removes the bill from the flow with a mandatory [reason].
  Future<bool> cancel(String reason) => _act(
        () => _repository.cancelBill(billId, reason),
        fallback: 'Não foi possível cancelar.',
        info: 'Boleto cancelado.',
      );
}
