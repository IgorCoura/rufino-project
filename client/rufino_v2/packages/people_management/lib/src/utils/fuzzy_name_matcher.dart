/// Fuzzy name matching utilities for Brazilian Portuguese names.
///
/// Provides accent-aware normalization, Jaro-Winkler similarity scoring,
/// and token-based overlap matching to correlate employee names found
/// inside PDF documents with known employee records.
library;

/// Confidence level for a fuzzy name match.
enum MatchConfidenceLevel {
  /// Confidence ≥ 0.85 — very likely the correct employee.
  high,

  /// Confidence ≥ 0.65 — probable match, should be reviewed.
  medium,

  /// Confidence ≥ 0.45 — weak match, needs manual verification.
  low,

  /// Confidence < 0.45 — no meaningful match found.
  none,
}

/// A candidate employee name to match against extracted text.
class NameCandidate {
  /// Creates a candidate with the identifiers needed for staging.
  const NameCandidate({
    required this.documentUnitId,
    required this.employeeId,
    required this.documentId,
    required this.name,
  });

  /// The document unit identifier.
  final String documentUnitId;

  /// The employee identifier.
  final String employeeId;

  /// The document identifier.
  final String documentId;

  /// The employee's full name as registered in the system.
  final String name;
}

/// The result of matching extracted text against a [NameCandidate].
class NameMatchResult {
  /// Creates a match result.
  const NameMatchResult({
    required this.candidate,
    required this.confidence,
    required this.confidenceLevel,
  });

  /// The best-matching candidate.
  final NameCandidate candidate;

  /// Similarity score between 0.0 (no match) and 1.0 (exact match).
  final double confidence;

  /// Categorical confidence level derived from [confidence].
  final MatchConfidenceLevel confidenceLevel;
}

/// Fuzzy name matcher optimised for Brazilian Portuguese.
///
/// Combines Jaro-Winkler similarity with token overlap scoring to handle
/// accent variations, name reordering, and missing middle names. Common
/// Portuguese connectors ("da", "de", "do", "dos", "das") are treated as
/// low-weight tokens that do not penalise the score when absent.
class FuzzyNameMatcher {
  FuzzyNameMatcher._();

  /// Portuguese name connectors that carry little discriminative value.
  static const _connectors = {'da', 'de', 'do', 'dos', 'das', 'e'};

  /// Labels that commonly precede an employee name in Brazilian documents.
  static final _nameLabels = RegExp(
    r'(?:nome|funcionário|funcionaria|colaborador|colaboradora|empregado|empregada|nome\s+(?:do|da)\s+(?:funcionário|funcionaria|colaborador|colaboradora|empregado|empregada))\s*[:]\s*',
    caseSensitive: false,
  );

  // ─── Public API ──────────────────────────────────────────

  /// Finds the best matching candidate for text extracted from a PDF.
  ///
  /// Scans [extractedText] for name-like sequences, scores each against
  /// every entry in [candidates], and returns the highest-scoring match.
  /// Returns `null` when no candidate reaches the minimum threshold.
  static NameMatchResult? findBestMatch(
    String extractedText,
    List<NameCandidate> candidates,
  ) {
    if (extractedText.isEmpty || candidates.isEmpty) return null;

    final nameFragments = _extractNameFragments(extractedText);
    if (nameFragments.isEmpty) {
      // Fallback: try matching the full normalized text directly.
      nameFragments.add(normalize(extractedText));
    }

    double bestScore = 0;
    NameCandidate? bestCandidate;

    for (final candidate in candidates) {
      final normalizedCandidate = normalize(candidate.name);
      if (normalizedCandidate.isEmpty) continue;

      for (final fragment in nameFragments) {
        final s = score(fragment, normalizedCandidate);
        if (s > bestScore) {
          bestScore = s;
          bestCandidate = candidate;
        }
      }

      // Also check if the candidate name appears as a substring of the
      // full extracted text (handles cases where label extraction misses).
      final normalizedText = normalize(extractedText);
      if (normalizedText.contains(normalizedCandidate)) {
        final substringScore = 0.80 + (0.20 * _tokenOverlap(
          normalizedCandidate,
          normalizedCandidate,
        ));
        if (substringScore > bestScore) {
          bestScore = substringScore;
          bestCandidate = candidate;
        }
      }
    }

    if (bestCandidate == null) return null;

    final clampedScore = bestScore.clamp(0.0, 1.0);
    // Do not return matches below the minimum threshold.
    if (clampedScore < 0.45) return null;

    return NameMatchResult(
      candidate: bestCandidate,
      confidence: clampedScore,
      confidenceLevel: _levelFrom(clampedScore),
    );
  }

  /// Computes a combined similarity score between two name strings.
  ///
  /// Both [name] and [candidate] should already be normalised (lowercase,
  /// no diacritics). The score blends Jaro-Winkler similarity (40 %),
  /// token overlap (40 %), and a substring containment bonus (20 %).
  static double score(String name, String candidate) {
    if (name.isEmpty || candidate.isEmpty) return 0;

    final jw = jaroWinkler(name, candidate);
    final tokenScore = _tokenOverlap(name, candidate);
    final containment = _containmentBonus(name, candidate);

    return (jw * 0.4) + (tokenScore * 0.4) + (containment * 0.2);
  }

  /// Normalises a string for comparison.
  ///
  /// Converts to lowercase, removes diacritics, strips non-letter/space
  /// characters, and collapses whitespace.
  static String normalize(String input) {
    var s = input.toLowerCase();
    s = _removeDiacritics(s);
    s = s.replaceAll(RegExp(r'[^a-z\s]'), ' ');
    s = s.replaceAll(RegExp(r'\s+'), ' ').trim();
    return s;
  }

  /// Computes the Jaro-Winkler similarity between [s1] and [s2].
  ///
  /// Returns a value between 0.0 (completely different) and 1.0 (identical).
  static double jaroWinkler(String s1, String s2) {
    if (s1 == s2) return 1.0;
    if (s1.isEmpty || s2.isEmpty) return 0.0;

    final maxDist = (s1.length > s2.length ? s1.length : s2.length) ~/ 2 - 1;
    if (maxDist < 0) return s1 == s2 ? 1.0 : 0.0;

    final s1Matches = List.filled(s1.length, false);
    final s2Matches = List.filled(s2.length, false);

    var matches = 0;
    var transpositions = 0;

    for (var i = 0; i < s1.length; i++) {
      final start = i - maxDist > 0 ? i - maxDist : 0;
      final end = i + maxDist + 1 < s2.length ? i + maxDist + 1 : s2.length;

      for (var j = start; j < end; j++) {
        if (s2Matches[j] || s1[i] != s2[j]) continue;
        s1Matches[i] = true;
        s2Matches[j] = true;
        matches++;
        break;
      }
    }

    if (matches == 0) return 0.0;

    var k = 0;
    for (var i = 0; i < s1.length; i++) {
      if (!s1Matches[i]) continue;
      while (!s2Matches[k]) {
        k++;
      }
      if (s1[i] != s2[k]) transpositions++;
      k++;
    }

    final jaro = (matches / s1.length +
            matches / s2.length +
            (matches - transpositions / 2) / matches) /
        3;

    // Winkler bonus for common prefix (up to 4 characters).
    var prefix = 0;
    final prefixLimit = s1.length < s2.length ? s1.length : s2.length;
    for (var i = 0; i < prefixLimit && i < 4; i++) {
      if (s1[i] == s2[i]) {
        prefix++;
      } else {
        break;
      }
    }

    return jaro + prefix * 0.1 * (1 - jaro);
  }

  // ─── Private helpers ──────────────────────────────────────

  /// Extracts plausible name fragments from [text].
  ///
  /// Looks for text following known labels (e.g. "Nome:", "Funcionário:")
  /// and also scans for sequences of capitalised words.
  static List<String> _extractNameFragments(String text) {
    final fragments = <String>[];

    // Strategy 1: text after known labels.
    for (final match in _nameLabels.allMatches(text)) {
      final afterLabel = text.substring(match.end).trim();
      // Take up to the next newline or 80 characters.
      final endIndex = afterLabel.indexOf(RegExp(r'[\n\r]'));
      final raw =
          endIndex >= 0 ? afterLabel.substring(0, endIndex) : afterLabel;
      final trimmed = raw.length > 80 ? raw.substring(0, 80) : raw;
      final normalized = normalize(trimmed);
      if (normalized.split(' ').length >= 2) {
        fragments.add(normalized);
      }
    }

    // Strategy 2: sequences of 2+ capitalised words (original casing).
    final capPattern = RegExp(
      r'\b([A-ZÁÀÂÃÉÈÊÍÏÓÔÕÚÙÛÇ][a-záàâãéèêíïóôõúùûç]+(?:\s+(?:da|de|do|dos|das|e)\s+)?(?:\s+[A-ZÁÀÂÃÉÈÊÍÏÓÔÕÚÙÛÇ][a-záàâãéèêíïóôõúùûç]+)+)',
    );
    for (final match in capPattern.allMatches(text)) {
      final normalized = normalize(match.group(0)!);
      if (normalized.split(' ').length >= 2 && !fragments.contains(normalized)) {
        fragments.add(normalized);
      }
    }

    return fragments;
  }

  /// Token overlap score ignoring Portuguese connectors.
  static double _tokenOverlap(String a, String b) {
    final tokensA =
        a.split(' ').where((t) => t.isNotEmpty && !_connectors.contains(t)).toSet();
    final tokensB =
        b.split(' ').where((t) => t.isNotEmpty && !_connectors.contains(t)).toSet();

    if (tokensA.isEmpty || tokensB.isEmpty) return 0;

    final intersection = tokensA.intersection(tokensB).length;
    final union = tokensA.union(tokensB).length;

    return intersection / union;
  }

  /// Returns 1.0 if one string fully contains the other, 0.0 otherwise.
  static double _containmentBonus(String a, String b) {
    if (a.contains(b) || b.contains(a)) return 1.0;
    return 0.0;
  }

  /// Maps a numeric score to a [MatchConfidenceLevel].
  static MatchConfidenceLevel _levelFrom(double score) {
    if (score >= 0.85) return MatchConfidenceLevel.high;
    if (score >= 0.65) return MatchConfidenceLevel.medium;
    if (score >= 0.45) return MatchConfidenceLevel.low;
    return MatchConfidenceLevel.none;
  }

  /// Replaces accented characters with their ASCII equivalents.
  static String _removeDiacritics(String input) {
    const map = <String, String>{
      'à': 'a', 'á': 'a', 'â': 'a', 'ã': 'a', 'ä': 'a', 'å': 'a',
      'è': 'e', 'é': 'e', 'ê': 'e', 'ë': 'e',
      'ì': 'i', 'í': 'i', 'î': 'i', 'ï': 'i',
      'ò': 'o', 'ó': 'o', 'ô': 'o', 'õ': 'o', 'ö': 'o',
      'ù': 'u', 'ú': 'u', 'û': 'u', 'ü': 'u',
      'ý': 'y', 'ÿ': 'y',
      'ñ': 'n', 'ç': 'c',
    };

    final buffer = StringBuffer();
    for (final char in input.runes) {
      final c = String.fromCharCode(char);
      buffer.write(map[c] ?? c);
    }
    return buffer.toString();
  }
}
