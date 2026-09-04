import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_enums.dart';
import '../../domain/bill_payment_exception.dart';
import '../../domain/bill_repository.dart';
import '../../domain/expectation.dart';
import '../../domain/expectation_repository.dart';
import '../../domain/payer_profile_repository.dart';

/// Stage of the pending panel.
enum PendingStatus {
  /// The panel is on its way.
  loading,

  /// The panel is on screen.
  loaded,

  /// The panel could not be loaded.
  error,
}

/// Drives the operator's daily panel: the approval queue count, the three
/// pending lists, and the onboarding nudge.
class PendingViewModel extends ChangeNotifier {
  /// Creates the view model.
  PendingViewModel({
    required ExpectationRepository expectationRepository,
    required BillRepository billRepository,
    required PayerProfileRepository payerProfileRepository,
  })  : _expectations = expectationRepository,
        _bills = billRepository,
        _payerProfile = payerProfileRepository;

  final ExpectationRepository _expectations;
  final BillRepository _bills;
  final PayerProfileRepository _payerProfile;

  PendingStatus _status = PendingStatus.loading;
  PendingExpectationsView _view = const PendingExpectationsView.empty();
  int _awaitingApprovalCount = 0;
  bool _awaitingApprovalTruncated = false;
  bool _missingPayerProfile = false;
  String? _errorMessage;

  /// The stage of the panel.
  PendingStatus get status => _status;

  /// The three pending lists.
  PendingExpectationsView get view => _view;

  /// How many bills wait for a decision (first page — see
  /// [awaitingApprovalTruncated]).
  int get awaitingApprovalCount => _awaitingApprovalCount;

  /// Whether the count above is a floor, not a total — the queue has more
  /// pages.
  bool get awaitingApprovalTruncated => _awaitingApprovalTruncated;

  /// Whether the payer profile is still missing — the onboarding banner.
  bool get missingPayerProfile => _missingPayerProfile;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Loads the panel: pending lists, approval queue and the profile check.
  Future<void> load() async {
    _status = PendingStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final results = await Future.wait([
      _expectations.getPending(),
      _bills.listBills(status: BillStatuses.awaitingApproval),
      _payerProfile.getProfile(),
    ]);

    results[0].fold(
      onSuccess: (view) => _view = view as PendingExpectationsView,
      onError: (error, _) {
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar as pendências.',
        );
      },
    );
    results[1].fold(
      onSuccess: (page) {
        final billPage = page as dynamic;
        _awaitingApprovalCount = (billPage.items as List).length;
        _awaitingApprovalTruncated = billPage.nextCursor != null;
      },
      onError: (_, __) {},
    );
    results[2].fold(
      onSuccess: (profile) => _missingPayerProfile = profile == null,
      onError: (_, __) {},
    );

    _status =
        _errorMessage == null ? PendingStatus.loaded : PendingStatus.error;
    notifyListeners();
  }
}
