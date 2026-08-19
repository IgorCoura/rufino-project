import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/capture_items/capture_item_detail_viewmodel.dart';
import 'package:bill_payment/src/ui/capture_items/capture_item_list_viewmodel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

void main() {
  group('CaptureItemListViewModel', () {
    late FakeCaptureItemRepository repository;
    late CaptureItemListViewModel viewModel;

    setUp(() {
      repository = FakeCaptureItemRepository();
      viewModel = CaptureItemListViewModel(repository: repository);
    });

    tearDown(() => viewModel.dispose());

    test('opens on the claim queue — the list a person can resolve',
        () async {
      repository.items = [
        captureItem(),
        captureItem(id: 'i2', status: CaptureItemStatuses.unrecognized),
      ];

      await viewModel.load();

      expect(repository.lastStatusFilter, CaptureItemStatuses.unrouted);
      expect(viewModel.items, hasLength(1));
    });

    test('selecting another status reloads under that filter', () async {
      repository.items = [
        captureItem(id: 'i2', status: CaptureItemStatuses.unrecognized),
      ];

      await viewModel.selectStatus(CaptureItemStatuses.unrecognized);

      expect(repository.lastStatusFilter, CaptureItemStatuses.unrecognized);
      expect(viewModel.items, hasLength(1));
    });

    test('selecting null asks for the whole list', () async {
      repository.items = [
        captureItem(),
        captureItem(id: 'i2', status: CaptureItemStatuses.unrecognized),
      ];

      await viewModel.selectStatus(null);

      expect(viewModel.items, hasLength(2));
    });
  });

  group('CaptureItemDetailViewModel', () {
    late FakeCaptureItemRepository repository;
    late CaptureItemDetailViewModel viewModel;

    setUp(() {
      repository = FakeCaptureItemRepository()..items = [captureItem()];
      viewModel = CaptureItemDetailViewModel(
        repository: repository,
        itemId: 'item-1',
      );
    });

    tearDown(() => viewModel.dispose());

    test('a successful claim exposes the new bill id for navigation',
        () async {
      await viewModel.load();

      final claimed = await viewModel.claim();

      expect(claimed, isTrue);
      expect(viewModel.claimedBillId, 'bill-7');
    });

    test('a contradicted claim surfaces the rule message and creates no '
        'navigation', () async {
      await viewModel.load();
      repository.setShouldFail(true);

      final claimed = await viewModel.claim();

      expect(claimed, isFalse);
      expect(viewModel.claimedBillId, isNull);
      expect(viewModel.errorMessage, 'regra disse não');
    });

    test('a reprocess records the action and reloads the item', () async {
      await viewModel.load();

      final reprocessed = await viewModel.reprocess();

      expect(reprocessed, isTrue);
      expect(repository.calls, contains('reprocessItem:item-1'));
      expect(viewModel.infoMessage, contains('segundo plano'));
    });
  });
}
