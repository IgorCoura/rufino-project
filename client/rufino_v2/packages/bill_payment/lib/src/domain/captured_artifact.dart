import 'dart:typed_data';

/// The original document as it arrived, ready to be shown.
///
/// It is held in memory and never written to disk: the artifact is the
/// evidence of what the system saw when it decided to pay, and a copy left in
/// the device's cache would outlive the session that was allowed to see it.
class CapturedArtifact {
  /// Creates the artifact record.
  const CapturedArtifact({
    required this.bytes,
    required this.contentType,
    this.fileName,
  });

  /// The document's bytes.
  final Uint8List bytes;

  /// The media type the server served, already without parameters.
  final String contentType;

  /// The name the server suggested, when it sent one.
  final String? fileName;

  /// Whether this is a PDF — the common case.
  bool get isPdf => contentType == 'application/pdf';

  /// Whether this is an image, which the mailbox delivers more often than one
  /// would guess: a photo of the bank slip, or a scanned page.
  bool get isImage => contentType.startsWith('image/');

  /// Whether the viewer can render it at all.
  bool get isViewable => isPdf || isImage;
}
