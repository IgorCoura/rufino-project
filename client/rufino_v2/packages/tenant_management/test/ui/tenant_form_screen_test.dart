import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

import '../fakes/fakes.dart';

void main() {
  late FakeTenantRepository repository;
  late List<String> registered;

  setUp(() {
    repository = FakeTenantRepository();
    registered = [];
  });

  Future<void> pumpForm(WidgetTester tester) async {
    // O formulário é longo e vive num ListView: numa janela de teste padrão
    // os últimos campos nem chegam a ser construídos.
    tester.view.physicalSize = const Size(1400, 4000);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        home: TenantFormScreen(
          viewModel: TenantFormViewModel(repository: repository),
          onRegistered: registered.add,
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  Future<void> fillValidCompany(WidgetTester tester) async {
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Razão social'),
      'Padaria do Zé LTDA',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'CNPJ'),
      '11222333000181',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'E-mail'),
      'contato@paoquente.com.br',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'CEP'),
      '30110000',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Logradouro'),
      'Rua das Flores',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Número'),
      '100',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Bairro'),
      'Centro',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Cidade'),
      'Belo Horizonte',
    );
    await tester.enterText(find.widgetWithText(TextFormField, 'UF'), 'mg');
    await tester.enterText(
      find.widgetWithText(TextFormField, 'E-mail do responsável'),
      'dono@paoquente.com.br',
    );
    await tester.pumpAndSettle();
  }

  Future<void> submit(WidgetTester tester) async {
    final button = find.widgetWithText(FilledButton, 'Cadastrar cliente');
    await tester.ensureVisible(button);
    await tester.tap(button);
    await tester.pumpAndSettle();
  }

  group('TenantFormScreen', () {
    testWidgets('a company is asked for a trade name and a CNPJ',
        (tester) async {
      await pumpForm(tester);

      expect(find.text('Razão social'), findsOneWidget);
      expect(find.text('Nome fantasia (opcional)'), findsOneWidget);
      expect(find.text('CNPJ'), findsOneWidget);
    });

    testWidgets('an individual has no trade name and is asked for a CPF',
        (tester) async {
      await pumpForm(tester);

      await tester.tap(find.text('Pessoa física'));
      await tester.pumpAndSettle();

      expect(find.text('Nome completo'), findsOneWidget);
      expect(find.text('Nome fantasia (opcional)'), findsNothing);
      expect(find.text('CPF'), findsOneWidget);
      expect(find.text('CNPJ'), findsNothing);
    });

    testWidgets('switching the kind clears the document already typed',
        (tester) async {
      await pumpForm(tester);

      await tester.enterText(
        find.widgetWithText(TextFormField, 'CNPJ'),
        '11222333000181',
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Pessoa física'));
      await tester.pumpAndSettle();

      final field = tester.widget<TextField>(
        find.descendant(
          of: find.ancestor(
            of: find.text('CPF'),
            matching: find.byType(TextFormField),
          ),
          matching: find.byType(TextField),
        ),
      );
      expect(field.controller?.text, isEmpty);
    });

    testWidgets('a document with wrong check digits blocks the submission',
        (tester) async {
      await pumpForm(tester);
      await fillValidCompany(tester);
      await tester.enterText(
        find.widgetWithText(TextFormField, 'CNPJ'),
        '11222333000182',
      );
      await tester.pumpAndSettle();

      await submit(tester);

      expect(find.text('CNPJ inválido.'), findsOneWidget);
      expect(repository.lastRegistered, isNull);
    });

    testWidgets('registering sends the cadastro and hands over the new id',
        (tester) async {
      await pumpForm(tester);
      await fillValidCompany(tester);
      await tester.tap(find.text('Contas a Pagar'));
      await tester.pumpAndSettle();

      await submit(tester);

      final sent = repository.lastRegistered!;
      expect(sent.kind, TenantKinds.company);
      expect(sent.legalName, 'Padaria do Zé LTDA');
      expect(sent.primaryTaxId, '11222333000181');
      expect(sent.ownerEmail, 'dono@paoquente.com.br');
      expect(sent.address.state, 'MG');
      expect(sent.products, [TenantProducts.billPayment]);
      expect(sent.id, isNull);
      expect(registered, ['new-tenant-id']);
    });

    testWidgets('a refused cadastro shows the message and goes nowhere',
        (tester) async {
      repository.setWriteShouldFail(
        true,
        message: 'Já existe cliente com este documento.',
      );

      await pumpForm(tester);
      await fillValidCompany(tester);
      await submit(tester);

      expect(find.text('Já existe cliente com este documento.'), findsOneWidget);
      expect(registered, isEmpty);
    });

    testWidgets('an informed id travels so a migration keeps its identity',
        (tester) async {
      await pumpForm(tester);
      await fillValidCompany(tester);

      await tester.tap(find.text('Avançado'));
      await tester.pumpAndSettle();
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Informar Id manualmente'),
        'id-existente',
      );
      await tester.pumpAndSettle();

      await submit(tester);

      expect(repository.lastRegistered?.id, 'id-existente');
    });
  });
}
