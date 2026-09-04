/// Stub implementation of [DocumentScannerService] for desktop platforms.
///
/// Document scanning is not supported on desktop. All operations return
/// empty or null values, and [isPlatformSupported] is always `false`.
library;

import 'dart:typed_data';
import 'package:people_management/people_management.dart';


/// Desktop stub that reports scanning as unsupported.
class DocumentScannerServiceImpl implements DocumentScannerService {
  @override
  bool get isPlatformSupported => false;

  @override
  Future<List<Uint8List>?> scanPages() async => null;

  @override
  Future<String> recognizeText(Uint8List imageBytes) async => '';

  @override
  Future<Uint8List> imagesToPdf(List<Uint8List> pages) async =>
      Uint8List(0);
}
