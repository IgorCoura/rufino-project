import 'dart:typed_data';

import '../../core/errors/document_scanner_exception.dart';
import '../../core/monitoring/error_reporter.dart';
import '../../core/result.dart';
import '../../core/utils/document_scanner_service.dart';
import '../../domain/repositories/document_scanner_repository.dart';

/// Concrete [DocumentScannerRepository] backed by [DocumentScannerService].
///
/// Wraps the service's `scanPages` call in `try/catch`, reports
/// unexpected errors to [ErrorReporter] and returns a typed
/// [Result]. Errors marked as `ExpectedFailure` (e.g. user denied a
/// permission) are short-circuited inside [ErrorReporter.capture] and
/// therefore never reach the crash dashboard.
class DocumentScannerRepositoryImpl implements DocumentScannerRepository {
  DocumentScannerRepositoryImpl({
    required this.scannerService,
    required this.reporter,
  });

  final DocumentScannerService scannerService;
  final ErrorReporter reporter;

  @override
  bool get isPlatformSupported => scannerService.isPlatformSupported;

  @override
  Future<Result<List<Uint8List>?>> scanPages() async {
    try {
      final pages = await scannerService.scanPages();
      return Result.success(pages);
    } on DocumentScannerException catch (e, st) {
      return reporter.failure(e, st);
    } catch (e, st) {
      return reporter.failure(ScannerPluginFailureException(e), st);
    }
  }

  @override
  Future<String> recognizeText(Uint8List imageBytes) =>
      scannerService.recognizeText(imageBytes);

  @override
  Future<Uint8List> imagesToPdf(List<Uint8List> pages) =>
      scannerService.imagesToPdf(pages);
}
