import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:rufino_v2/data/services/http_exception.dart';
import 'package:rufino_v2/data/services/multipart_upload_helper.dart';

void main() {
  group('sendMultipartWithProgress', () {
    test('reports progress from 0.0 to 1.0 during upload', () async {
      final client = MockClient.streaming((request, bodyStream) async {
        await bodyStream.toBytes();
        return http.StreamedResponse(Stream.value([]), 200);
      });

      final uri = Uri.parse('https://example.com/upload');
      final request = http.MultipartRequest('POST', uri);
      request.files.add(http.MultipartFile.fromBytes(
        'file',
        List.filled(1024, 0),
        filename: 'test.bin',
      ));

      final progressValues = <double>[];

      await sendMultipartWithProgress(
        request,
        client: client,
        onProgress: (p) => progressValues.add(p),
      );

      expect(progressValues, isNotEmpty);
      expect(progressValues.last, closeTo(1.0, 0.01));
      expect(
        progressValues,
        everyElement(
          allOf(greaterThanOrEqualTo(0.0), lessThanOrEqualTo(1.0)),
        ),
      );

      client.close();
    });

    test('throws HttpException on non-2xx response', () async {
      final client = MockClient.streaming((request, bodyStream) async {
        await bodyStream.toBytes();
        return http.StreamedResponse(Stream.value([]), 500);
      });

      final uri = Uri.parse('https://example.com/upload');
      final request = http.MultipartRequest('POST', uri);
      request.files.add(http.MultipartFile.fromBytes(
        'file',
        [1, 2, 3],
        filename: 'test.bin',
      ));

      expect(
        () => sendMultipartWithProgress(request, client: client),
        throwsA(isA<HttpException>()),
      );

      client.close();
    });

    test('completes successfully without onProgress callback', () async {
      final client = MockClient.streaming((request, bodyStream) async {
        await bodyStream.toBytes();
        return http.StreamedResponse(Stream.value([]), 200);
      });

      final uri = Uri.parse('https://example.com/upload');
      final request = http.MultipartRequest('POST', uri);
      request.files.add(http.MultipartFile.fromBytes(
        'file',
        [1, 2, 3],
        filename: 'test.bin',
      ));

      final response = await sendMultipartWithProgress(
        request,
        client: client,
      );

      expect(response.statusCode, 200);

      client.close();
    });

    test('progress values are monotonically increasing', () async {
      final client = MockClient.streaming((request, bodyStream) async {
        await bodyStream.toBytes();
        return http.StreamedResponse(Stream.value([]), 200);
      });

      final uri = Uri.parse('https://example.com/upload');
      final request = http.MultipartRequest('POST', uri);
      request.files.add(http.MultipartFile.fromBytes(
        'file',
        List.filled(4096, 0),
        filename: 'large.bin',
      ));

      final progressValues = <double>[];

      await sendMultipartWithProgress(
        request,
        client: client,
        onProgress: (p) => progressValues.add(p),
      );

      for (int i = 1; i < progressValues.length; i++) {
        expect(
          progressValues[i],
          greaterThanOrEqualTo(progressValues[i - 1]),
          reason: 'progress should never decrease',
        );
      }

      client.close();
    });
  });
}
