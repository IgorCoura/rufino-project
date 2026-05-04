import 'expected_failure.dart';

/// Base sealed class for all document-scanner errors.
///
/// Subtypes are thrown by [DocumentScannerService] implementations and
/// caught at the repository boundary, where they are wrapped in
/// [Result.error]. They are never propagated past the data layer.
sealed class DocumentScannerException implements Exception {
  const DocumentScannerException();
}

/// Thrown when the user denied a permission required by the scanner
/// (camera or photo library) for the current session.
///
/// Treated as [ExpectedFailure] because it is a user-actionable mistake,
/// not a bug — it must not pollute the crash dashboard.
final class ScannerPermissionDeniedException extends DocumentScannerException
    with ExpectedFailure {
  const ScannerPermissionDeniedException();
}

/// Thrown when a permission required by the scanner has been
/// permanently denied and the user must enable it from the iOS Settings
/// app before the scanner can be opened again.
///
/// Treated as [ExpectedFailure] for the same reason as
/// [ScannerPermissionDeniedException].
final class ScannerPermissionPermanentlyDeniedException
    extends DocumentScannerException with ExpectedFailure {
  const ScannerPermissionPermanentlyDeniedException();
}

/// Thrown when the native document scanner plugin throws or fails to
/// present its UI (e.g. the iOS `VNDocumentCameraViewController` cannot
/// be presented over the current view controller).
final class ScannerPluginFailureException extends DocumentScannerException {
  const ScannerPluginFailureException(this.cause);

  /// The underlying error returned by the native plugin.
  final Object cause;
}

/// Thrown when reading the bytes of one of the temporary image files
/// returned by the native scanner fails (e.g. sandbox restrictions, the
/// path was already cleaned up by iOS, or the file is not readable).
final class ScannerFileReadException extends DocumentScannerException {
  const ScannerFileReadException(this.path, this.cause);

  /// The file path the scanner returned.
  final String path;

  /// The underlying error from the file system.
  final Object cause;
}
