import 'dart:convert';

import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mocktail/mocktail.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

class MockCaptureItemApiService extends Mock implements CaptureItemApiService {}

class MockBillApiService extends Mock implements BillApiService {}

http.Response documentResponse({
  String? contentType = 'application/pdf',
  String? disposition,
}) {
  return http.Response.bytes(
    utf8.encode('%PDF-1.4 documento'),
    200,
    headers: {
      if (contentType != null) 'content-type': contentType,
      if (disposition != null) 'content-disposition': disposition,
    },
  );
}

void main() {
  group('artifactFromResponse', () {
    test('carries the bytes and the media type the server served', () {
      final artifact = artifactFromResponse(documentResponse());

      expect(artifact.contentType, 'application/pdf');
      expect(artifact.isPdf, isTrue);
      expect(utf8.decode(artifact.bytes), startsWith('%PDF'));
    });

    // Charset no cabeçalho não muda o que o documento é — sem descartar o
    // parâmetro, um PDF servido com charset cairia no ramo "formato não
    // exibido" da tela.
    test('strips the parameters from the media type', () {
      final artifact = artifactFromResponse(
        documentResponse(contentType: 'application/pdf; charset=utf-8'),
      );

      expect(artifact.contentType, 'application/pdf');
    });

    test('classifies an image as viewable without being a pdf', () {
      final artifact =
          artifactFromResponse(documentResponse(contentType: 'image/jpeg'));

      expect(artifact.isPdf, isFalse);
      expect(artifact.isImage, isTrue);
      expect(artifact.isViewable, isTrue);
    });

    // Sem tipo, o padrão faz o documento cair no ramo honesto da tela em vez
    // de tentar renderizar bytes como PDF.
    test('falls back to a neutral media type when the header is absent', () {
      final artifact = artifactFromResponse(documentResponse(contentType: null));

      expect(artifact.contentType, 'application/octet-stream');
      expect(artifact.isViewable, isFalse);
    });

    test('reads the suggested file name from the disposition header', () {
      final artifact = artifactFromResponse(
        documentResponse(disposition: 'attachment; filename="boleto-enel.pdf"'),
      );

      expect(artifact.fileName, 'boleto-enel.pdf');
    });

    // O ASP.NET Core escreve as duas formas quando o nome tem acento, e é a
    // estrelada que chega íntegra.
    test('prefers the RFC 5987 name when both forms are present', () {
      final artifact = artifactFromResponse(
        documentResponse(
          disposition:
              "attachment; filename=boleto.pdf; filename*=UTF-8''fatura%20energia.pdf",
        ),
      );

      expect(artifact.fileName, 'fatura energia.pdf');
    });

    test('leaves the name null when the server did not suggest one', () {
      expect(artifactFromResponse(documentResponse()).fileName, isNull);
    });
  });

  group('CaptureItemRepositoryImpl.getArtifact', () {
    late MockCaptureItemApiService apiService;
    late FakeErrorReporter reporter;
    late CaptureItemRepositoryImpl repository;

    setUp(() {
      apiService = MockCaptureItemApiService();
      reporter = FakeErrorReporter();
      repository = CaptureItemRepositoryImpl(
        apiService: apiService,
        reporter: reporter,
      );
    });

    test('returns the document the service downloaded', () async {
      when(() => apiService.getArtifact('item-1'))
          .thenAnswer((_) async => artifact());

      final result = await repository.getArtifact('item-1');

      result.fold(
        onSuccess: (value) => expect(value.isPdf, isTrue),
        onError: (error, _) => fail('should have succeeded: $error'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });

    // 404 é a resposta normal para "não há documento" e para "você não pode
    // ver este item" — é regra do servidor, não defeito, e não vai ao monitor.
    test('does not report a 404 answered by the server', () async {
      when(() => apiService.getArtifact('item-1')).thenThrow(
        const HttpException(
          statusCode: 404,
          message: 'HTTP 404: Not Found',
          serverMessages: ['Documento não disponível.'],
        ),
      );

      final result = await repository.getArtifact('item-1');

      expect(result.isError, isTrue);
      expect(reporter.capturedErrors, isEmpty);
    });

    test('reports an unexpected failure', () async {
      when(() => apiService.getArtifact('item-1'))
          .thenThrow(Exception('socket morreu'));

      final result = await repository.getArtifact('item-1');

      expect(result.isError, isTrue);
      expect(reporter.capturedErrors, hasLength(1));
    });
  });

  group('BillRepositoryImpl.getArtifact', () {
    late MockBillApiService apiService;
    late FakeErrorReporter reporter;
    late BillRepositoryImpl repository;

    setUp(() {
      apiService = MockBillApiService();
      reporter = FakeErrorReporter();
      repository = BillRepositoryImpl(
        apiService: apiService,
        reporter: reporter,
      );
    });

    test('returns the document the service downloaded', () async {
      when(() => apiService.getArtifact('bill-1'))
          .thenAnswer((_) async => artifact());

      final result = await repository.getArtifact('bill-1');

      result.fold(
        onSuccess: (value) => expect(value.isPdf, isTrue),
        onError: (error, _) => fail('should have succeeded: $error'),
      );
      expect(reporter.capturedErrors, isEmpty);
    });
  });
}
