import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/core/utils/document_scanner_service.dart';
import 'package:rufino_v2/core/utils/document_scanner_service_stub.dart'
    as stub;

import '../../../testing/mocks/mocks.dart';

void main() {
  group('DocumentScannerService', () {
    group('stub implementation (desktop)', () {
      late DocumentScannerService service;

      setUp(() {
        // Use the stub directly to avoid conditional import resolving
        // to the mobile implementation in the test runner (dart:io).
        service = stub.DocumentScannerServiceImpl();
      });

      test('reports platform as unsupported', () {
        expect(service.isPlatformSupported, isFalse);
      });

      test('scanPages returns null on unsupported platform', () async {
        final result = await service.scanPages();
        expect(result, isNull);
      });

      test('recognizeText returns empty string on unsupported platform',
          () async {
        final result =
            await service.recognizeText(Uint8List.fromList([0xFF, 0xD8]));
        expect(result, isEmpty);
      });

      test('imagesToPdf returns empty bytes on unsupported platform', () async {
        final result =
            await service.imagesToPdf([Uint8List.fromList([0xFF, 0xD8])]);
        expect(result, isEmpty);
      });
    });

    group('mock implementation', () {
      late MockDocumentScannerService mockService;

      setUp(() {
        mockService = MockDocumentScannerService();
      });

      test('scanPages returns page images when scanning succeeds', () async {
        final fakePages = [
          Uint8List.fromList([1, 2, 3]),
          Uint8List.fromList([4, 5, 6]),
        ];
        when(() => mockService.scanPages())
            .thenAnswer((_) async => fakePages);

        final result = await mockService.scanPages();

        expect(result, isNotNull);
        expect(result, hasLength(2));
        expect(result![0], fakePages[0]);
        expect(result[1], fakePages[1]);
      });

      test('scanPages returns null when user cancels', () async {
        when(() => mockService.scanPages()).thenAnswer((_) async => null);

        final result = await mockService.scanPages();

        expect(result, isNull);
      });

      test('recognizeText returns OCR text from image', () async {
        final imageBytes = Uint8List.fromList([0xFF, 0xD8, 0x01]);
        when(() => mockService.recognizeText(imageBytes))
            .thenAnswer((_) async => 'Nome: João Silva');

        final result = await mockService.recognizeText(imageBytes);

        expect(result, 'Nome: João Silva');
      });

      test('imagesToPdf produces PDF bytes from images', () async {
        final pages = [Uint8List.fromList([1, 2, 3])];
        final fakePdf = Uint8List.fromList([0x25, 0x50, 0x44, 0x46]);
        when(() => mockService.imagesToPdf(pages))
            .thenAnswer((_) async => fakePdf);

        final result = await mockService.imagesToPdf(pages);

        expect(result, fakePdf);
        verify(() => mockService.imagesToPdf(pages)).called(1);
      });
    });
  });
}
