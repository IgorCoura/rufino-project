import 'package:http/http.dart' as http;

import '../domain/captured_artifact.dart';

/// Reads the media type and the suggested name off a document response.
///
/// Shared by the two endpoints that serve artifacts — quarantine item and
/// bill — because they answer with the same shape, and parsing the headers
/// twice is how the two would drift apart.
CapturedArtifact artifactFromResponse(http.Response response) {
  return CapturedArtifact(
    bytes: response.bodyBytes,
    contentType: _mediaType(response.headers['content-type']),
    fileName: _fileName(response.headers['content-disposition']),
  );
}

/// Strips the parameters: `application/pdf; charset=utf-8` is still a PDF.
String _mediaType(String? header) {
  final value = header?.split(';').first.trim().toLowerCase();
  return (value == null || value.isEmpty) ? 'application/octet-stream' : value;
}

/// Pulls the name out of `Content-Disposition`, preferring the RFC 5987 form.
///
/// ASP.NET Core writes both `filename` and `filename*` whenever the name has
/// a non-ASCII character, and the starred one is the one that survived the
/// trip intact.
String? _fileName(String? header) {
  if (header == null) return null;

  final extended = RegExp("filename\\*=(?:UTF-8'')?([^;]+)", caseSensitive: false)
      .firstMatch(header);
  if (extended != null) {
    return Uri.decodeComponent(extended.group(1)!.trim());
  }

  final plain = RegExp('filename="?([^";]+)"?', caseSensitive: false)
      .firstMatch(header);
  return plain?.group(1)?.trim();
}
