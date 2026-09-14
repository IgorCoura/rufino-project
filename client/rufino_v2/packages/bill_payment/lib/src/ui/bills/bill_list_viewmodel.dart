import 'dart:collection';

import 'package:flutter/foundation.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/bill.dart';
import '../../domain/bill_payment_enums.dart';
import '../../domain/bill_payment_exception.dart';
import '../../domain/bill_repository.dart';
import '../shared/document_picker.dart';

/// Stage of the bill listing.
enum BillListStatus {
  /// First page on its way.
  loading,

  /// Rows on screen.
  loaded,

  /// Another page on its way, rows already on screen.
  loadingMore,

  /// Nothing under the current filter.
  empty,

  /// The listing could not be loaded.
  error,
}

/// What the documents export sheet asked for.
typedef BillDocumentExportOptions = ({
  String pages,
  bool includeReceipts,
  String packaging,
});

/// Drives the bill listing, filtered by status on the server, and the
/// selection that downloads several bills' documents at once.
class BillListViewModel extends ChangeNotifier {
  /// Creates the view model, optionally opening on [initialStatus].
  ///
  /// [onSaveDocument] e [reporter] sustentam o download em lote. Sem o
  /// salvador a seleção continua funcionando, mas não há o que baixar — é o
  /// caso de uma casca que não emprestou a capacidade.
  BillListViewModel({
    required BillRepository repository,
    String? initialStatus,
    DocumentSaver? onSaveDocument,
    ErrorReporter? reporter,
  })  : _repository = repository,
        _statusFilter = initialStatus,
        _onSaveDocument = onSaveDocument,
        _reporter = reporter;

  final BillRepository _repository;
  final DocumentSaver? _onSaveDocument;
  final ErrorReporter? _reporter;

  final List<Bill> _items = [];
  BillListStatus _status = BillListStatus.loading;
  String? _statusFilter;
  String? _nextCursor;
  String? _errorMessage;

  // LinkedHashMap de propósito: a ordem em que a pessoa marcou é a ordem do
  // arquivo baixado (decisão de 2026-09-14).
  final LinkedHashMap<String, Bill> _selected = LinkedHashMap();
  bool _isExporting = false;
  String? _exportMessage;

  /// The rows currently loaded.
  UnmodifiableListView<Bill> get items => UnmodifiableListView(_items);

  /// The stage of the listing.
  BillListStatus get status => _status;

  /// The status filter in force, or `null` for everything.
  String? get statusFilter => _statusFilter;

  /// The message of the last failure.
  String? get errorMessage => _errorMessage;

  /// Whether there is another page to ask for.
  bool get hasMore => _nextCursor != null;

  /// Whether at least one bill is selected — the list is in selection mode.
  bool get isSelecting => _selected.isNotEmpty;

  /// The selected bills, in the order they were selected.
  List<Bill> get selectedBills => List.unmodifiable(_selected.values);

  /// Whether the documents of the selection can be downloaded at all.
  bool get canExport => _onSaveDocument != null;

  /// Whether a download is on its way.
  bool get isExporting => _isExporting;

  /// The outcome of the last download, for a one-off snackbar.
  String? get exportMessage => _exportMessage;

  /// Whether the bill [id] is selected.
  bool isSelected(String id) => _selected.containsKey(id);

  /// Selects [bill], or unselects it when it already was.
  void toggleSelection(Bill bill) {
    if (_selected.remove(bill.id) == null) _selected[bill.id] = bill;
    notifyListeners();
  }

  /// Selects every row ALREADY LOADED, keeping the order of the ones selected
  /// before. The list is paginated: what was not loaded is not selected.
  void selectAllLoaded() {
    for (final bill in _items) {
      _selected.putIfAbsent(bill.id, () => bill);
    }
    notifyListeners();
  }

  /// Leaves selection mode.
  void clearSelection() {
    if (_selected.isEmpty) return;
    _selected.clear();
    notifyListeners();
  }

  /// Forgets the last download message once the screen has shown it.
  void clearExportMessage() => _exportMessage = null;

  /// Loads the first page under the current filter.
  Future<void> load() async {
    _status = BillListStatus.loading;
    _errorMessage = null;
    _nextCursor = null;
    notifyListeners();

    final result = await _repository.listBills(status: _statusFilter);
    result.fold(
      onSuccess: (page) {
        _items
          ..clear()
          ..addAll(page.items);
        _nextCursor = page.nextCursor;
        _status =
            _items.isEmpty ? BillListStatus.empty : BillListStatus.loaded;
      },
      onError: (error, _) {
        _status = BillListStatus.error;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar os boletos.',
        );
      },
    );
    notifyListeners();
  }

  /// Loads the next page, keeping what is already on screen.
  Future<void> loadMore() async {
    final cursor = _nextCursor;
    if (cursor == null || _status == BillListStatus.loadingMore) return;

    _status = BillListStatus.loadingMore;
    notifyListeners();

    final result = await _repository.listBills(
      status: _statusFilter,
      cursor: cursor,
    );
    result.fold(
      onSuccess: (page) {
        _items.addAll(page.items);
        _nextCursor = page.nextCursor;
        _status = BillListStatus.loaded;
      },
      onError: (error, _) {
        _status = BillListStatus.loaded;
        _errorMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível carregar mais boletos.',
        );
      },
    );
    notifyListeners();
  }

  /// Selects the status filter (`null` = everything) and reloads.
  ///
  /// Trocar o filtro LIMPA a seleção: manter boletos marcados que saíram da
  /// vista faria o download levar o que a pessoa não está vendo.
  Future<void> selectStatus(String? status) {
    _statusFilter = status;
    _selected.clear();
    return load();
  }

  /// Downloads the documents of the selection with [options].
  ///
  /// Salvou → a seleção é limpa, o trabalho acabou. Desistir da caixa de
  /// diálogo do sistema não é erro e não vira "Arquivo salvo.". Falha do
  /// servidor mantém a seleção, para tentar de novo sem remarcar tudo.
  Future<void> exportSelected(BillDocumentExportOptions options) async {
    final save = _onSaveDocument;
    if (save == null || _isExporting || _selected.isEmpty) return;

    _isExporting = true;
    _exportMessage = null;
    notifyListeners();

    final result = await _repository.exportDocuments(
      billIds: _selected.keys.toList(),
      pages: options.pages,
      includeReceipts: options.includeReceipts,
      packaging: options.packaging,
    );

    await result.fold(
      onSuccess: (file) async {
        try {
          final saved = await save(
            fileName: file.fileName ?? _fallbackFileName(options.packaging),
            bytes: file.bytes,
          );
          if (saved) {
            _exportMessage = 'Arquivo salvo.';
            _selected.clear();
          }
        } catch (error, stackTrace) {
          // Excecao NOMEADA da regra, como no visualizador: quem falhou foi o
          // plugin de plataforma, nao o repositorio. O nome do arquivo NAO
          // entra no contexto — ele carrega beneficiario e vencimento.
          _reporter?.capture(
            error,
            stackTrace,
            context: {'op': 'bills.exportDocuments.save'},
          );
          _exportMessage = 'Não foi possível salvar o arquivo.';
        }
      },
      onError: (error, _) async {
        _exportMessage = billPaymentErrorMessage(
          error,
          fallback: 'Não foi possível baixar os documentos.',
        );
      },
    );

    _isExporting = false;
    notifyListeners();
  }

  static String _fallbackFileName(String packaging) =>
      packaging == BillDocumentPackagings.pdfPerBill
          ? 'boletos.zip'
          : 'boletos.pdf';
}
