/// Detects the orientation of a scanned page by comparing OCR text lengths
/// across the four cardinal rotations.
///
/// Used by the batch document scanning flow to recover pages that were
/// captured rotated 90°, 180°, or 270°: the OCR engine returns very little
/// text on misoriented pages, so the rotation with the longest recognized
/// text is the one most likely to be upright.
library;

import 'package:flutter/foundation.dart';
import 'package:image/image.dart' as img;

/// The orientation chosen for a page after comparing OCR text lengths.
class RotationCandidate {
  /// Creates a candidate result.
  const RotationCandidate({
    required this.bytes,
    required this.text,
    required this.rotationDegrees,
  });

  /// Page bytes in the chosen orientation.
  ///
  /// When [rotationDegrees] is `0` these are the original bytes passed in;
  /// otherwise they are a freshly encoded JPEG of the rotated image.
  final Uint8List bytes;

  /// OCR text recognized for the chosen orientation.
  final String text;

  /// Rotation that was applied to the original page, in degrees clockwise.
  ///
  /// One of `0`, `90`, `180`, `270`.
  final int rotationDegrees;
}

/// Picks the orientation that yields the longest OCR text for a page.
///
/// Rotates [originalBytes] in 90°, 180°, and 270° (decoding once and using
/// [img.copyRotate] inside a background isolate), runs [recognizeText] on
/// each rotated variant, and compares the resulting text lengths against
/// [originalText] (which must already be the OCR output of [originalBytes]
/// at 0°).
///
/// Returns the candidate with the maximum `text.length`. Ties go to the
/// smaller rotation angle, so 0° wins any tie — callers can detect "no
/// rotation needed" via `result.rotationDegrees == 0`, in which case
/// `result.bytes` is identical to [originalBytes] and `result.text` is
/// [originalText].
///
/// Returns `null` when [originalBytes] cannot be decoded as an image; the
/// caller should keep the original page untouched in that case.
Future<RotationCandidate?> pickBestRotation({
  required Uint8List originalBytes,
  required String originalText,
  required Future<String> Function(Uint8List) recognizeText,
}) async {
  final rotations = await compute(_rotateAt90Steps, originalBytes);
  if (rotations == null) return null;

  final candidates = <RotationCandidate>[
    RotationCandidate(
      bytes: originalBytes,
      text: originalText,
      rotationDegrees: 0,
    ),
  ];

  for (final entry in rotations.entries) {
    final text = await recognizeText(entry.value);
    candidates.add(RotationCandidate(
      bytes: entry.value,
      text: text,
      rotationDegrees: entry.key,
    ));
  }

  RotationCandidate winner = candidates.first;
  for (final candidate in candidates.skip(1)) {
    if (candidate.text.length > winner.text.length) {
      winner = candidate;
    }
  }
  return winner;
}

/// Decodes [bytes] once and returns the encoded JPEG bytes for rotations
/// 90°, 180°, and 270°.
///
/// Returns `null` when the image cannot be decoded. Runs entirely inside a
/// background isolate.
Map<int, Uint8List>? _rotateAt90Steps(Uint8List bytes) {
  final img.Image? decoded;
  try {
    decoded = img.decodeImage(bytes);
  } catch (_) {
    return null;
  }
  if (decoded == null) return null;

  return {
    for (final angle in const [90, 180, 270])
      angle: Uint8List.fromList(
        img.encodeJpg(
          img.copyRotate(decoded, angle: angle),
          quality: 90,
        ),
      ),
  };
}
