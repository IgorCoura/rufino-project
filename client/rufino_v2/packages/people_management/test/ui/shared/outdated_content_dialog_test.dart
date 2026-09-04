import 'package:people_management/people_management.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import '../../fakes/fake_permission_repository.dart';
import 'package:rufino_core/rufino_core.dart';

void main() {
  late FakePermissionRepository repository;
  late PermissionNotifier notifier;
  OutdatedContentAction? action;

  const rows = [
    OutdatedDocumentRow(
      title: 'Maria Silva',
      subtitle: '15/03/2026',
      isOutdated: true,
    ),
    OutdatedDocumentRow(
      title: 'João Souza',
      subtitle: '16/03/2026',
      isOutdated: false,
    ),
  ];

  setUp(() async {
    action = null;
    repository = FakePermissionRepository()
      ..setPermissions([
        const Permission(resource: 'document', scopes: ['edit', 'generate']),
      ]);
    notifier = PermissionNotifier(permissionRepository: repository);
    await notifier.loadPermissions();
  });

  tearDown(() => notifier.dispose());

  /// Pumps a screen with a single button that opens the dialog, taps it, and
  /// leaves the dialog on screen with its result captured in [action].
  Future<void> openDialog(
    WidgetTester tester, {
    bool allowRefresh = true,
    List<OutdatedDocumentRow> dialogRows = rows,
  }) async {
    await tester.pumpWidget(
      ChangeNotifierProvider<PermissionNotifier>.value(
        value: notifier,
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => TextButton(
                onPressed: () async {
                  action = await showOutdatedContentDialog(
                    context,
                    rows: dialogRows,
                    allowRefresh: allowRefresh,
                  );
                },
                child: const Text('abrir'),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('abrir'));
    await tester.pumpAndSettle();
  }

  group('showOutdatedContentDialog', () {
    testWidgets('flags each outdated document individually', (tester) async {
      await openDialog(tester);

      expect(find.text('Maria Silva'), findsOneWidget);
      expect(find.text('João Souza'), findsOneWidget);
      expect(find.text('Desatualizado'), findsOneWidget);
      expect(find.textContaining('1 de 2 documento(s)'), findsOneWidget);
    });

    testWidgets('returns continueAnyway when the user generates as is',
        (tester) async {
      await openDialog(tester);

      await tester.tap(find.text('Gerar com os dados atuais'));
      await tester.pumpAndSettle();

      expect(action, OutdatedContentAction.continueAnyway);
    });

    testWidgets('returns refreshAndContinue when the user updates first',
        (tester) async {
      await openDialog(tester);

      await tester.tap(find.text('Atualizar e gerar'));
      await tester.pumpAndSettle();

      expect(action, OutdatedContentAction.refreshAndContinue);
    });

    testWidgets('returns cancel when the user backs out', (tester) async {
      await openDialog(tester);

      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(action, OutdatedContentAction.cancel);
    });

    testWidgets('offers no refresh action in batch mode', (tester) async {
      await openDialog(tester, allowRefresh: false);

      expect(find.text('Atualizar e gerar'), findsNothing);
      expect(find.text('Gerar assim mesmo'), findsOneWidget);
      expect(
        find.textContaining('edite o documento do funcionário'),
        findsOneWidget,
      );
    });

    testWidgets('hides the refresh action without the document edit scope',
        (tester) async {
      repository.setPermissions([
        const Permission(resource: 'document', scopes: ['generate']),
      ]);
      await notifier.loadPermissions();

      await openDialog(tester);

      expect(find.text('Atualizar e gerar'), findsNothing);
      expect(find.text('Gerar com os dados atuais'), findsOneWidget);
    });
  });
}
