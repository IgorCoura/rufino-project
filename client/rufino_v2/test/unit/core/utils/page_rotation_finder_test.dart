import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:image/image.dart' as img;
import 'package:rufino_v2/core/utils/page_rotation_finder.dart';

/// Encodes a 60×40 solid-colour JPEG. Asymmetric dimensions let us verify
/// that 90°/270° rotations swap width and height.
Uint8List _jpegBytes({int width = 60, int height = 40}) {
  final image = img.Image(width: width, height: height);
  img.fill(image, color: img.ColorRgb8(120, 200, 80));
  return Uint8List.fromList(img.encodeJpg(image));
}

void main() {
  group('pickBestRotation', () {
    test('picks rotation with the longest OCR text', () async {
      final original = _jpegBytes();
      final ocrByAngle = <int, String>{
        90: 'João da Silva Costa',
        180: 'X',
        270: '',
      };
      var callCount = 0;
      final winner = await pickBestRotation(
        originalBytes: original,
        originalText: 'Joao',
        recognizeText: (bytes) async {
          callCount++;
          // Compare bytes by length only — the exact bytes returned by the
          // image package are not stable enough to identify rotations
          // directly. Instead, we serve replies in the deterministic order
          // 90°, 180°, 270° in which [pickBestRotation] iterates the map.
          return ocrByAngle.values.elementAt(callCount - 1);
        },
      );

      expect(winner, isNotNull);
      expect(winner!.rotationDegrees, equals(90));
      expect(winner.text, equals('João da Silva Costa'));
      expect(callCount, equals(3));
    });

    test('returns 0° when original text is already the longest', () async {
      final original = _jpegBytes();
      final winner = await pickBestRotation(
        originalBytes: original,
        originalText: 'João da Silva Costa Pereira',
        recognizeText: (_) async => 'x',
      );

      expect(winner, isNotNull);
      expect(winner!.rotationDegrees, equals(0));
      expect(winner.text, equals('João da Silva Costa Pereira'));
      expect(winner.bytes, same(original));
    });

    test('ties go to the smaller angle (0° beats any rotation on tie)',
        () async {
      final original = _jpegBytes();
      final winner = await pickBestRotation(
        originalBytes: original,
        originalText: 'abcd',
        recognizeText: (_) async => 'abcd', // every rotation ties
      );

      expect(winner, isNotNull);
      expect(winner!.rotationDegrees, equals(0));
      expect(winner.bytes, same(original));
    });

    test('between rotations, the smaller angle wins a tie', () async {
      final original = _jpegBytes();
      final ocrByAngle = ['LONGER TEXT A', 'LONGER TEXT B', 'short'];
      var i = 0;
      final winner = await pickBestRotation(
        originalBytes: original,
        originalText: '',
        recognizeText: (_) async => ocrByAngle[i++],
      );

      expect(winner, isNotNull);
      // 90° and 180° tie at length 13; both beat originalText (length 0)
      // and 270° (length 5). 90° wins the tie because it was tested first.
      expect(winner!.rotationDegrees, equals(90));
      expect(winner.text, equals('LONGER TEXT A'));
    });

    test('returns null when decoding fails', () async {
      var calls = 0;
      final winner = await pickBestRotation(
        originalBytes: Uint8List.fromList([0, 1, 2, 3]),
        originalText: 'whatever',
        recognizeText: (_) async {
          calls++;
          return '';
        },
      );

      expect(winner, isNull);
      expect(calls, equals(0));
    });

    test('does not re-OCR the original page', () async {
      final original = _jpegBytes();
      final seenBytes = <Uint8List>[];
      await pickBestRotation(
        originalBytes: original,
        originalText: 'orig',
        recognizeText: (bytes) async {
          seenBytes.add(bytes);
          return '';
        },
      );

      expect(seenBytes, hasLength(3));
      for (final bytes in seenBytes) {
        expect(identical(bytes, original), isFalse);
      }
    });

    test('rotated bytes have inverted dimensions for 90° and 270°',
        () async {
      final original = _jpegBytes(width: 60, height: 40);
      final capturedBytes = <Uint8List>[];
      // Force the winner to be 90° by giving it the longest text.
      final ocrByAngle = ['NINETY NINETY NINETY', 'x', ''];
      var i = 0;
      final winner = await pickBestRotation(
        originalBytes: original,
        originalText: '',
        recognizeText: (bytes) async {
          capturedBytes.add(bytes);
          return ocrByAngle[i++];
        },
      );

      expect(winner!.rotationDegrees, equals(90));

      final ninety = img.decodeImage(capturedBytes[0])!;
      expect(ninety.width, equals(40));
      expect(ninety.height, equals(60));

      final oneEighty = img.decodeImage(capturedBytes[1])!;
      expect(oneEighty.width, equals(60));
      expect(oneEighty.height, equals(40));

      final twoSeventy = img.decodeImage(capturedBytes[2])!;
      expect(twoSeventy.width, equals(40));
      expect(twoSeventy.height, equals(60));
    });
  });
}
