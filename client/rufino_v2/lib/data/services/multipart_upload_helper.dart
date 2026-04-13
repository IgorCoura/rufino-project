import 'dart:async';

import 'package:http/http.dart' as http;

import 'http_exception.dart';

/// Callback invoked during multipart upload with progress from 0.0 to 1.0.
typedef UploadProgressCallback = void Function(double progress);

/// Sends a [http.MultipartRequest] while reporting upload progress via
/// [onProgress].
///
/// Uses [client] to send the request. Wraps the request's byte stream to
/// count bytes written and calculates the fraction of total content length
/// sent. Throws [HttpException] on non-2xx responses.
Future<http.StreamedResponse> sendMultipartWithProgress(
  http.MultipartRequest request, {
  required http.Client client,
  UploadProgressCallback? onProgress,
}) async {
  final totalBytes = request.contentLength;
  final byteStream = request.finalize();

  int sentBytes = 0;

  final progressStream = byteStream.transform(
    StreamTransformer<List<int>, List<int>>.fromHandlers(
      handleData: (data, sink) {
        sink.add(data);
        sentBytes += data.length;
        if (onProgress != null && totalBytes > 0) {
          onProgress((sentBytes / totalBytes).clamp(0.0, 1.0));
        }
      },
    ),
  );

  final streamedRequest = http.StreamedRequest(request.method, request.url)
    ..headers.addAll(request.headers)
    ..contentLength = totalBytes;

  progressStream.listen(
    streamedRequest.sink.add,
    onDone: streamedRequest.sink.close,
    onError: streamedRequest.sink.addError,
    cancelOnError: true,
  );

  final response = await client.send(streamedRequest);

  if (response.statusCode < 200 || response.statusCode >= 300) {
    throw HttpException(
      statusCode: response.statusCode,
      message: 'HTTP ${response.statusCode}',
    );
  }

  return response;
}
