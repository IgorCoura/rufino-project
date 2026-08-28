import 'package:bill_payment/src/ui/shared/artifact_viewer_viewmodel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../fakes/fakes.dart';

void main() {
  group('ArtifactViewerViewModel', () {
    late FakeCaptureItemRepository repository;

    setUp(() => repository = FakeCaptureItemRepository());

    ArtifactViewerViewModel viewModelFor(String id) => ArtifactViewerViewModel(
          load: () => repository.getArtifact(id),
        );

    test('starts loading before anything is asked', () {
      final viewModel = viewModelFor('item-1');
      addTearDown(viewModel.dispose);

      expect(viewModel.status, ArtifactViewerStatus.loading);
      expect(viewModel.artifact, isNull);
    });

    test('holds the document once the loader answers', () async {
      final viewModel = viewModelFor('item-1');
      addTearDown(viewModel.dispose);

      await viewModel.load();

      expect(viewModel.status, ArtifactViewerStatus.loaded);
      expect(viewModel.artifact!.isPdf, isTrue);
      expect(repository.calls, contains('getArtifact:item-1'));
    });

    // O 404 do servidor cobre "não há arquivo" e "você não pode ver este item"
    // com a mesma resposta, de propósito — então a tela diz a única coisa
    // verdadeira nos dois casos, em vez de inventar um motivo.
    test('shows an honest message when the document is unavailable', () async {
      repository.setShouldFail(true);
      final viewModel = viewModelFor('item-1');
      addTearDown(viewModel.dispose);

      await viewModel.load();

      expect(viewModel.status, ArtifactViewerStatus.error);
      expect(viewModel.errorMessage, isNotNull);
      expect(viewModel.artifact, isNull);
    });

    test('a retry after a failure clears the message and loads', () async {
      repository.setShouldFail(true);
      final viewModel = viewModelFor('item-1');
      addTearDown(viewModel.dispose);
      await viewModel.load();

      repository.setShouldFail(false);
      await viewModel.load();

      expect(viewModel.status, ArtifactViewerStatus.loaded);
      expect(viewModel.errorMessage, isNull);
    });

    // A mesma tela serve item de quarentena e boleto: o que muda é o loader,
    // e é isso que a impede de conhecer os dois repositórios.
    test('serves a bill through the same view model', () async {
      final bills = FakeBillRepository();
      final viewModel =
          ArtifactViewerViewModel(load: () => bills.getArtifact('bill-1'));
      addTearDown(viewModel.dispose);

      await viewModel.load();

      expect(viewModel.status, ArtifactViewerStatus.loaded);
      expect(bills.calls, contains('getArtifact:bill-1'));
    });
  });
}
