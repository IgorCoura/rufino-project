import 'package:bill_payment/bill_payment.dart';
import 'package:bill_payment/src/ui/payees/payee_detail_viewmodel.dart';
import 'package:bill_payment/src/ui/payees/payee_form_viewmodel.dart';
import 'package:bill_payment/src/ui/payees/payee_list_viewmodel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

void main() {
  group('PayeeListViewModel', () {
    late FakePayeeRepository repository;
    late PayeeListViewModel viewModel;

    setUp(() {
      repository = FakePayeeRepository();
      viewModel = PayeeListViewModel(repository: repository);
    });

    tearDown(() => viewModel.dispose());

    test('loads the first page and lands on loaded', () async {
      repository.payees = [payee(), payee(id: 'payee-2')];

      await viewModel.load();

      expect(viewModel.status, PayeeListStatus.loaded);
      expect(viewModel.items, hasLength(2));
    });

    test('an empty cadastro lands on empty', () async {
      await viewModel.load();

      expect(viewModel.status, PayeeListStatus.empty);
    });

    test('a failed load lands on error with a message', () async {
      repository.setShouldFail(true);

      await viewModel.load();

      expect(viewModel.status, PayeeListStatus.error);
      expect(viewModel.errorMessage, 'regra disse não');
    });

    test('loadMore appends the next page without dropping rows', () async {
      repository
        ..payees = List.generate(5, (i) => payee(id: 'payee-$i'))
        ..pageSize = 2;

      await viewModel.load();
      await viewModel.loadMore();

      expect(viewModel.items, hasLength(4));
      expect(viewModel.hasMore, isTrue);
    });

    test('a failed loadMore keeps the rows already on screen', () async {
      repository
        ..payees = List.generate(4, (i) => payee(id: 'payee-$i'))
        ..pageSize = 2;
      await viewModel.load();

      repository.setShouldFail(true);
      await viewModel.loadMore();

      expect(viewModel.status, PayeeListStatus.loaded);
      expect(viewModel.items, hasLength(2));
      expect(viewModel.errorMessage, isNotNull);
    });

    test('the document search shows the single match and clears back to the '
        'list', () async {
      repository.payees = [payee(), payee(id: 'p2', taxId: '111.444.777-35')];

      await viewModel.searchByTaxId('02.302.100/0001-06');
      expect(viewModel.items, hasLength(1));
      expect(viewModel.isSearching, isTrue);

      await viewModel.searchByTaxId('');
      expect(viewModel.items, hasLength(2));
      expect(viewModel.isSearching, isFalse);
    });

    test('a document nobody registered lands on empty, not error', () async {
      repository.payees = [payee()];

      await viewModel.searchByTaxId('999.999.999-99');

      expect(viewModel.status, PayeeListStatus.empty);
      expect(viewModel.errorMessage, isNull);
    });
  });

  group('PayeeFormViewModel', () {
    late FakePayeeRepository repository;
    late PayeeFormViewModel viewModel;

    setUp(() {
      repository = FakePayeeRepository();
      viewModel = PayeeFormViewModel(repository: repository);
    });

    tearDown(() => viewModel.dispose());

    test('registers and resolves to the new id', () async {
      final id = await viewModel.register(
        legalName: 'EDP',
        taxId: '02.302.100/0001-06',
      );

      expect(id, 'payee-new');
      expect(viewModel.isSaving, isFalse);
      expect(viewModel.errorMessage, isNull);
    });

    test('a refused registration surfaces the rule message and resolves to '
        'null', () async {
      repository.setShouldFail(true);

      final id = await viewModel.register(
        legalName: 'EDP',
        taxId: '02.302.100/0001-06',
      );

      expect(id, isNull);
      expect(viewModel.errorMessage, 'regra disse não');
      expect(viewModel.isSaving, isFalse);
    });

    test('only the selected policy kind sends its fields', () async {
      viewModel.selectPolicyKind(AmountPolicyKinds.fixed);

      await viewModel.register(
        legalName: 'EDP',
        taxId: '02.302.100/0001-06',
        expectedAmount: 100,
        minAmount: 1,
        maxAmount: 2,
      );

      expect(repository.calls, contains('registerPayee:EDP'));
    });
  });

  group('PayeeDetailViewModel', () {
    late FakePayeeRepository repository;
    late PayeeDetailViewModel viewModel;

    setUp(() {
      repository = FakePayeeRepository()..payees = [payee()];
      viewModel = PayeeDetailViewModel(
        repository: repository,
        payeeId: 'payee-1',
      );
    });

    tearDown(() => viewModel.dispose());

    test('loads the cadastro', () async {
      await viewModel.load();

      expect(viewModel.status, PayeeDetailStatus.loaded);
      expect(viewModel.payee!.legalName, 'EDP SAO PAULO SA');
    });

    // savePolicy era código morto: existia no view model e nenhuma tela o
    // chamava. Agora o card de política de valor o usa.
    test('savePolicy manda os campos do tipo escolhido e recarrega', () async {
      await viewModel.load();

      final saved = await viewModel.savePolicy(
        const AmountPolicyInput(
          kind: AmountPolicyKinds.fixed,
          expectedAmount: 1500,
          tolerancePercent: 5,
        ),
      );

      expect(saved, isTrue);
      expect(repository.calls, contains('changeAmountPolicy:Fixed'));
      expect(repository.lastPolicy!.expectedAmount, 1500);
      expect(repository.lastPolicy!.tolerancePercent, 5);
      expect(viewModel.status, PayeeDetailStatus.loaded);
    });

    // Recusa do domínio (faixa invertida, tolerância fora de 0..100) não sai da
    // edição e deixa a mensagem na tela.
    test('savePolicy recusada reporta e devolve false', () async {
      await viewModel.load();
      repository.setShouldFail(true);

      final saved = await viewModel.savePolicy(
        const AmountPolicyInput(
          kind: AmountPolicyKinds.range,
          minAmount: 400,
          maxAmount: 80,
        ),
      );

      expect(saved, isFalse);
      expect(viewModel.errorMessage, isNotNull);
    });

    test('a successful mutation records the write and reloads', () async {
      await viewModel.load();

      final saved = await viewModel.addAlias('BANDEIRANTE');

      expect(saved, isTrue);
      expect(repository.calls, contains('addAlias:BANDEIRANTE'));
      expect(viewModel.status, PayeeDetailStatus.loaded);
    });

    test('a refused mutation keeps the cadastro on screen with the message',
        () async {
      await viewModel.load();
      repository.setShouldFail(true);

      final saved = await viewModel.saveLegalName('NOVO NOME');

      expect(saved, isFalse);
      expect(viewModel.errorMessage, 'regra disse não');
      expect(viewModel.status, PayeeDetailStatus.loaded);
      expect(viewModel.payee, isNotNull);
    });

    test('deleting resolves true without reloading a gone cadastro',
        () async {
      await viewModel.load();

      final deleted = await viewModel.deletePayee();

      expect(deleted, isTrue);
      expect(repository.calls, contains('deletePayee:payee-1'));
    });
  });
}
