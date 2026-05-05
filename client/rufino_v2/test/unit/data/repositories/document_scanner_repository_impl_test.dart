import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rufino_v2/core/errors/document_scanner_exception.dart';
import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/data/repositories/document_scanner_repository_impl.dart';

import '../../../testing/fakes/fake_error_reporter.dart';
import '../../../testing/mocks/mocks.dart';

void main() {
  group('DocumentScannerRepositoryImpl', () {
    late MockDocumentScannerService scannerService;
    late FakeErrorReporter reporter;
    late DocumentScannerRepositoryImpl repository;

    setUp(() {
      scannerService = MockDocumentScannerService();
      reporter = FakeErrorReporter();
      repository = DocumentScannerRepositoryImpl(
        scannerService: scannerService,
        reporter: reporter,
      );
      registerFallbackValue(Uint8List(0));
      registerFallbackValue(<Uint8List>[]);
    });

    test('isPlatformSupported delegates to the underlying service', () {
      when(() => scannerService.isPlatformSupported).thenReturn(true);
      expect(repository.isPlatformSupported, isTrue);

      when(() => scannerService.isPlatformSupported).thenReturn(false);
      expect(repository.isPlatformSupported, isFalse);
    });

    group('scanPages', () {
      test('returns Success with the page bytes when scanning succeeds',
          () async {
        final pages = [
          Uint8List.fromList([0xFF, 0xD8, 0x01]),
          Uint8List.fromList([0xFF, 0xD8, 0x02]),
        ];
        when(() => scannerService.scanPages()).thenAnswer((_) async => pages);

        final result = await repository.scanPages();

        expect(result, isA<Success<List<Uint8List>?>>());
        expect((result as Success<List<Uint8List>?>).value, pages);
        expect(reporter.capturedErrors, isEmpty);
      });

      test('returns Success(null) when the user cancels (service returns null)',
          () async {
        when(() => scannerService.scanPages()).thenAnswer((_) async => null);

        final result = await repository.scanPages();

        expect(result, isA<Success<List<Uint8List>?>>());
        expect((result as Success<List<Uint8List>?>).value, isNull);
        expect(reporter.capturedErrors, isEmpty);
      });

      test(
        'returns a Failure carrying ScannerPermissionDeniedException without '
        'reporting it (it is an expected user-actionable failure)',
        () async {
          when(() => scannerService.scanPages())
              .thenThrow(const ScannerPermissionDeniedException());

          final result = await repository.scanPages();

          expect(result, isA<Failure<List<Uint8List>?>>());
          expect(
            (result as Failure<List<Uint8List>?>).error,
            isA<ScannerPermissionDeniedException>(),
          );
          expect(reporter.capturedErrors, isEmpty);
        },
      );

      test(
        'returns a Failure carrying ScannerPermissionPermanentlyDeniedException '
        'without reporting it',
        () async {
          when(() => scannerService.scanPages())
              .thenThrow(const ScannerPermissionPermanentlyDeniedException());

          final result = await repository.scanPages();

          expect(result, isA<Failure<List<Uint8List>?>>());
          expect(
            (result as Failure<List<Uint8List>?>).error,
            isA<ScannerPermissionPermanentlyDeniedException>(),
          );
          expect(reporter.capturedErrors, isEmpty);
        },
      );

      test(
        'returns a Failure carrying ScannerPluginFailureException AND reports '
        'it with a context map that surfaces the underlying native cause, '
        'because the plugin error is a bug we need to triage in Sentry',
        () async {
          final cause = Exception('VNDocumentCameraViewController failed');
          when(() => scannerService.scanPages())
              .thenThrow(ScannerPluginFailureException(cause));

          final result = await repository.scanPages();

          expect(result, isA<Failure<List<Uint8List>?>>());
          final failure = result as Failure<List<Uint8List>?>;
          expect(failure.error, isA<ScannerPluginFailureException>());
          expect(reporter.capturedErrors, hasLength(1));
          final captured = reporter.capturedErrors.single;
          expect(captured.error, isA<ScannerPluginFailureException>());
          expect(captured.context?['op'], 'scanPages');
          expect(
            captured.context?['cause'] as String?,
            contains('VNDocumentCameraViewController failed'),
          );
        },
      );

      test(
        'returns a Failure carrying ScannerFileReadException and reports it '
        'with the path and the underlying cause attached as context',
        () async {
          when(() => scannerService.scanPages()).thenThrow(
            const ScannerFileReadException('/tmp/page.jpg', 'sandbox error'),
          );

          final result = await repository.scanPages();

          expect(result, isA<Failure<List<Uint8List>?>>());
          expect(
            (result as Failure<List<Uint8List>?>).error,
            isA<ScannerFileReadException>(),
          );
          expect(reporter.capturedErrors, hasLength(1));
          final captured = reporter.capturedErrors.single;
          expect(captured.context?['op'], 'scanPages');
          expect(captured.context?['path'], '/tmp/page.jpg');
          expect(captured.context?['cause'], contains('sandbox error'));
        },
      );

      test(
        'wraps an unexpected raw exception in ScannerPluginFailureException '
        'and forwards its toString as context so the native error is visible '
        'in the crash dashboard',
        () async {
          final cause = StateError('something went sideways');
          when(() => scannerService.scanPages()).thenThrow(cause);

          final result = await repository.scanPages();

          expect(result, isA<Failure<List<Uint8List>?>>());
          final failure = result as Failure<List<Uint8List>?>;
          final error = failure.error;
          expect(error, isA<ScannerPluginFailureException>());
          expect((error as ScannerPluginFailureException).cause, same(cause));
          expect(reporter.capturedErrors, hasLength(1));
          final captured = reporter.capturedErrors.single;
          expect(captured.context?['op'], 'scanPages');
          expect(
            captured.context?['cause'] as String?,
            contains('something went sideways'),
          );
        },
      );
    });

    test('recognizeText delegates straight to the service', () async {
      final imageBytes = Uint8List.fromList([0xFF, 0xD8]);
      when(() => scannerService.recognizeText(imageBytes))
          .thenAnswer((_) async => 'Nome: João Silva');

      expect(await repository.recognizeText(imageBytes), 'Nome: João Silva');
      verify(() => scannerService.recognizeText(imageBytes)).called(1);
    });

    test('imagesToPdf delegates straight to the service', () async {
      final pages = [Uint8List.fromList([1, 2, 3])];
      final pdfBytes = Uint8List.fromList([0x25, 0x50, 0x44, 0x46]);
      when(() => scannerService.imagesToPdf(pages))
          .thenAnswer((_) async => pdfBytes);

      expect(await repository.imagesToPdf(pages), pdfBytes);
      verify(() => scannerService.imagesToPdf(pages)).called(1);
    });
  });
}
