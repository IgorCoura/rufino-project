/// Mobile/desktop OCR backend for [extractLastDocumentDate].
///
/// Writes the image bytes to a temporary file (ML Kit requires a path),
/// runs latin text recognition, and returns the raw recognised text.
library;

import 'dart:io';
import 'dart:typed_data';

import 'package:google_mlkit_text_recognition/google_mlkit_text_recognition.dart';

/// Runs ML Kit text recognition on [bytes] and returns the recognised text.
///
/// Returns `null` on any error (unsupported platform, decode failure, ML
/// Kit initialisation failure, etc.). Never throws.
Future<String?> recognizeImageText(Uint8List bytes) async {
  File? tempFile;
  TextRecognizer? recognizer;
  try {
    final dir = Directory.systemTemp;
    final path =
        '${dir.path}/rufino_ocr_${DateTime.now().microsecondsSinceEpoch}.bin';
    tempFile = await File(path).writeAsBytes(bytes, flush: true);
    final input = InputImage.fromFilePath(tempFile.path);
    recognizer = TextRecognizer(script: TextRecognitionScript.latin);
    final result = await recognizer.processImage(input);
    return result.text;
  } catch (_) {
    return null;
  } finally {
    await recognizer?.close();
    if (tempFile != null) {
      try {
        await tempFile.delete();
      } catch (_) {/* best-effort cleanup */}
    }
  }
}
