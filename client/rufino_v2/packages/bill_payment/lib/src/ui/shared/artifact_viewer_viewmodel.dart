import 'package:flutter/foundation.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/bill_payment_exception.dart';
import '../../domain/captured_artifact.dart';
import 'document_picker.dart';

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
  ///
  /// [onSave] e [suggestedFileName] sustentam o download: o primeiro e a
  /// capacidade emprestada pela casca, o segundo e o nome que a Page montou a
  /// partir do que ela sabe do boleto — quem le esta tela so tem os bytes.
  ArtifactViewerViewModel({
    required Future<dynamic> Function() load,
    required DocumentSaver onSave,
    required ErrorReporter reporter,
    String? suggestedFileName,
  })  : _load = load,
        _onSave = onSave,
        _reporter = reporter,
        _suggestedFileName = suggestedFileName;

  final Future<dynamic> Function() _load;
  final DocumentSaver _onSave;
  final ErrorReporter _reporter;
  final String? _suggestedFileName;

  ArtifactViewerStatus _status = ArtifactViewerStatus.loading;
  CapturedArtifact? _artifact;
  String? _errorMessage;
  bool _isSaving = false;
  String? _infoMessage;

  /// The stage of the viewer.
  ArtifactViewerStatus get status => _status;

  /// The document, once loaded.
  CapturedArtifact? get artifact => _artifact;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Whether a download is in flight.
  bool get isSaving => _isSaving;

  /// O aviso do ultimo download, para a tela mostrar num SnackBar.
  String? get infoMessage => _infoMessage;

  /// Whether the document can be downloaded.
  ///
  /// Nao pergunta se da para EXIBIR: o artefato que a tela nao renderiza e
  /// justamente o que mais precisa do download, porque baixar e a unica saida
  /// que sobra para quem precisa do papel.
  bool get canSave =>
      _status == ArtifactViewerStatus.loaded && _artifact != null;

  /// O nome com que o arquivo sera salvo.
  ///
  /// Ordem: o nome amigavel que a Page montou, o que o servidor sugeriu no
  /// `Content-Disposition`, e por fim um generico — sempre com a extensao do
  /// tipo de midia, porque arquivo sem extensao nao abre com dois cliques.
  String get downloadFileName {
    final artifact = _artifact;
    final chosen = _firstNonBlank([
      _suggestedFileName,
      artifact?.fileName,
      'documento',
    ]);

    return artifact == null
        ? chosen
        : ensureExtension(
            chosen,
            artifact.contentType,
            borrowFrom: artifact.fileName,
          );
  }

  /// Baixa o documento que esta na tela.
  ///
  /// Nao ha requisicao nova: os bytes ja vieram para renderizar, e salvar e
  /// grava-los. Desistir da caixa de dialogo do sistema nao e erro e nao vira
  /// aviso de sucesso.
  Future<void> save() async {
    final artifact = _artifact;

    // O toque duplo nao salva duas vezes, e sem artefato nao ha o que salvar.
    if (_isSaving || artifact == null) return;

    _isSaving = true;
    _infoMessage = null;
    notifyListeners();

    try {
      final saved = await _onSave(
        fileName: downloadFileName,
        bytes: artifact.bytes,
      );

      if (saved) _infoMessage = 'Arquivo salvo.';
    } catch (error, stackTrace) {
      // Excecao do ViewModel e a excecao NOMEADA da regra: quem falhou nao foi
      // repositorio (que reportaria sozinho), foi um plugin de plataforma.
      //
      // O nome do arquivo NAO entra no contexto — ele carrega beneficiario e
      // conta, e relatorio de erro nao e lugar para isso.
      _reporter.capture(
        error,
        stackTrace,
        context: {'op': 'artifact.save', 'contentType': artifact.contentType},
      );
      _infoMessage = 'Não foi possível salvar o arquivo.';
    } finally {
      _isSaving = false;
      notifyListeners();
    }
  }

  static String _firstNonBlank(List<String?> candidates) =>
      candidates.firstWhere(
        (c) => c != null && c.trim().isNotEmpty,
        orElse: () => 'documento',
      )!.trim();

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
