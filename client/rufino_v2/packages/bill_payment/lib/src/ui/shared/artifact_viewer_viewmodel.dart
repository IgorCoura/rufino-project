import 'package:flutter/foundation.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/captured_artifact.dart';

/// Stage of the document viewer.
enum ArtifactViewerStatus {
  /// The document is on its way.
  loading,

  /// The document is on screen.
  loaded,

  /// The document could not be loaded.
  error,
}

/// Drives the document viewer for whatever fetches the bytes.
///
/// It takes a loader instead of a repository because the same screen serves
/// two origins — a quarantine item and a bill — and they are different
/// repositories. Teaching this view model about both would make it the one
/// place that knows the whole module.
class ArtifactViewerViewModel extends ChangeNotifier {
  /// Creates the view model over [load].
  ArtifactViewerViewModel({required Future<dynamic> Function() load})
      : _load = load;

  final Future<dynamic> Function() _load;

  ArtifactViewerStatus _status = ArtifactViewerStatus.loading;
  CapturedArtifact? _artifact;
  String? _errorMessage;

  /// The stage of the viewer.
  ArtifactViewerStatus get status => _status;

  /// The document, once loaded.
  CapturedArtifact? get artifact => _artifact;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Loads the document.
  Future<void> load() async {
    _status = ArtifactViewerStatus.loading;
    _errorMessage = null;
    notifyListeners();

    final result = await _load();
    (result as dynamic).fold(
      onSuccess: (artifact) {
        _artifact = artifact as CapturedArtifact;
        _status = ArtifactViewerStatus.loaded;
      },
      onError: (error, _) {
        _status = ArtifactViewerStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          // O 404 do servidor cobre "não há arquivo" e "você não pode ver
          // este item" com a mesma resposta, de propósito — então a tela diz
          // a única coisa verdadeira nos dois casos.
          fallback: 'O documento original não está disponível.',
        );
      },
    );
    notifyListeners();
  }
}
