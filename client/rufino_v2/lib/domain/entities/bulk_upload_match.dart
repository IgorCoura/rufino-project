/// Represents the result of matching a single uploaded file to an employee.
///
/// Holds the file data, cached extracted text, and the best-matching
/// employee assignment. Mutable assignment fields allow the user to
/// manually reassign during the verification step.
library;

import 'dart:typed_data';

import '../../core/utils/fuzzy_name_matcher.dart';

/// A file that has been matched (or not) to a pending document unit.
///
/// Created during the bulk upload flow when the user selects multiple
/// PDFs at once. The [extractedText] is cached at creation time so that
/// subsequent previews are instantaneous.
class BulkUploadMatch {
  /// Creates a bulk upload match entry.
  BulkUploadMatch({
    required this.fileName,
    required this.fileBytes,
    required this.extractedText,
    this.matchedDocumentUnitId,
    this.matchedEmployeeName,
    this.matchedEmployeeId,
    this.matchedDocumentId,
    this.confidence = 0.0,
    this.confidenceLevel = MatchConfidenceLevel.none,
  });

  /// The original file name selected by the user.
  final String fileName;

  /// The raw file bytes held in memory until staging.
  final Uint8List fileBytes;

  /// Text extracted from the PDF, cached for instant preview.
  final String extractedText;

  /// The document unit this file is assigned to, or `null` if unmatched.
  String? matchedDocumentUnitId;

  /// The matched employee's full name for display.
  String? matchedEmployeeName;

  /// The matched employee identifier.
  String? matchedEmployeeId;

  /// The matched document identifier.
  String? matchedDocumentId;

  /// Similarity score between 0.0 and 1.0.
  double confidence;

  /// Categorical confidence level derived from [confidence].
  MatchConfidenceLevel confidenceLevel;

  /// Whether this file has been assigned to a document unit.
  bool get isMatched => matchedDocumentUnitId != null;

  /// Whether this match requires manual review before staging.
  bool get needsReview => confidenceLevel != MatchConfidenceLevel.high;
}
