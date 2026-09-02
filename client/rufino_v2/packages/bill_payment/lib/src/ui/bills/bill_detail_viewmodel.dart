import 'package:flutter/foundation.dart';

import '../../domain/bill_detail.dart';
import '../../domain/bill_payment_enums.dart';
import '../../domain/bill_payment_exception.dart';
import '../../domain/bill_repository.dart';
import '../../domain/payment_order.dart';
import '../../domain/payment_repository.dart';

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
    PaymentRepository? paymentRepository,
    DateTime Function()? clock,
  })  : _repository = repository,
        _paymentRepository = paymentRepository,
        _clock = clock ?? DateTime.now;

  final BillRepository _repository;
  final PaymentRepository? _paymentRepository;
  final DateTime Function() _clock;

  /// The bill being shown.
  final String billId;

  BillDetail? _bill;
  PaymentOrder? _payment;
  BillDetailStatus _status = BillDetailStatus.loading;
  String? _errorMessage;
  String? _infoMessage;
  bool _isMutating = false;

  /// The bill, once loaded.
  BillDetail? get bill => _bill;

  /// The payment order behind the bill, once the approval produced one.
  ///
  /// Null is a normal state — before an approval, and in the observable
  /// window while the outbox has not created the order yet.
  PaymentOrder? get payment => _payment;

  /// Whether the bill is overdue right now — the ADR-017 consent gate.
  bool get isOverdue => _bill?.isOverdueAt(_clock()) ?? false;

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

    await _loadPayment();
  }

  /// Loads the payment order when the bill has (or may have) one.
  ///
  /// Failure here never breaks the detail: the bill is on screen either way,
  /// and the section simply says the execution could not be read.
  Future<void> _loadPayment() async {
    final repository = _paymentRepository;
    final status = _bill?.status;
    if (repository == null || status == null) return;

    final committed = status == BillStatuses.approved ||
        status == BillStatuses.scheduled ||
        status == BillStatuses.paid ||
        status == BillStatuses.failed;
    if (!committed) {
      _payment = null;
      return;
    }

    final result = await repository.getForBill(billId);
    result.fold(
      onSuccess: (order) => _payment = order,
      onError: (_, __) => _payment = null,
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
    bool acknowledgeImmediateExecution = false,
  }) =>
      _act(
        () => _repository.approveBill(
          billId,
          scheduleFor: scheduleFor,
          note: note,
          acknowledgeRisk: acknowledgeRisk,
          acknowledgeImmediateExecution: acknowledgeImmediateExecution,
        ),
        fallback: 'Não foi possível aprovar.',
        info: 'Pagamento autorizado.',
      );

  /// Returns the FAILED bill to the decision queue (new approval, new order).
  Future<bool> reopen() => _act(
        () => _repository.reopenBill(billId),
        fallback: 'Não foi possível reabrir.',
        info: 'Boleto devolvido à fila de decisão.',
      );

  /// Cancels the payment order — the reaction window in action.
  Future<bool> cancelPayment() {
    final repository = _paymentRepository;
    final orderId = _payment?.id;
    if (repository == null || orderId == null) return Future.value(false);

    return _act(
      () => repository.cancel(orderId),
      fallback: 'Não foi possível cancelar o agendamento.',
      info: 'Agendamento cancelado.',
    );
  }

  /// Confirms the immediate (overdue) payment the order is waiting on.
  Future<bool> confirmImmediatePayment() {
    final repository = _paymentRepository;
    final orderId = _payment?.id;
    if (repository == null || orderId == null) return Future.value(false);

    return _act(
      () => repository.confirmImmediate(orderId),
      fallback: 'Não foi possível confirmar o pagamento.',
      info: 'Pagamento imediato confirmado — a fila retoma em instantes.',
    );
  }

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
