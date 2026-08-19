import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/trusted_origins/trusted_origin_list_viewmodel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

void main() {
  late FakeTrustedOriginRepository repository;
  late TrustedOriginListViewModel viewModel;

  setUp(() {
    repository = FakeTrustedOriginRepository();
    viewModel = TrustedOriginListViewModel(repository: repository);
  });

  tearDown(() => viewModel.dispose());

  group('TrustedOriginListViewModel', () {
    test('loads the first page and lands on loaded', () async {
      repository.origins = [trustedOrigin()];

      await viewModel.load();

      expect(viewModel.status, TrustedOriginListStatus.loaded);
      expect(viewModel.items, hasLength(1));
    });

    test('loadMore appends without repeating', () async {
      repository
        ..origins = List.generate(3, (i) => trustedOrigin(id: 'o$i'))
        ..pageSize = 2;

      await viewModel.load();
      await viewModel.loadMore();

      expect(viewModel.items, hasLength(3));
      expect(viewModel.hasMore, isFalse);
    });

    test('an unknown sender resolves to unknown, not error', () async {
      repository.origins = [trustedOrigin()];

      await viewModel.resolveSender('alguem@desconhecido.com.br');

      expect(viewModel.resolveOutcome, ResolveOutcome.unknown);
      expect(viewModel.errorMessage, isNull);
    });

    test('a matched sender exposes the origin that answered', () async {
      repository.origins = [trustedOrigin()];

      await viewModel.resolveSender('cobranca@fornecedor.com.br');

      expect(viewModel.resolveOutcome, ResolveOutcome.matched);
      expect(viewModel.resolved!.value, 'fornecedor.com.br');
    });

    test('registering reloads the list on success', () async {
      final registered = await viewModel.register(
        kind: OriginKinds.emailDomain,
        value: 'fornecedor.com.br',
        decision: TrustDecisions.trusted,
      );

      expect(registered, isTrue);
      expect(repository.calls, contains('registerOrigin:fornecedor.com.br'));
    });

    test('a refused decision change surfaces the rule message', () async {
      repository.origins = [trustedOrigin()];
      await viewModel.load();
      repository.setShouldFail(true);

      final changed =
          await viewModel.changeDecision('origin-1', TrustDecisions.blocked);

      expect(changed, isFalse);
      expect(viewModel.errorMessage, 'regra disse não');
      expect(viewModel.items, hasLength(1),
          reason: 'the rows on screen must survive a refused mutation');
    });
  });
}
