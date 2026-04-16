import '../../domain/entities/batch_download.dart';

/// Builds the combined PDF file name following the backend naming pattern.
///
/// The pattern is `{YYYY_MM_DD}-{EMPLOYEE}-{DOCUMENT}-{idSuffix}.PDF`,
/// mirroring `BatchDownloadQueries.DownloadBatchDocumentUnits` in the server.
/// All segments are UPPERCASED and spaces are replaced with underscores.
///
/// Uses the [firstUnit]'s date, employee name, template name, and the last
/// 4 characters of its document unit ID as the suffix.
String buildCombinedFileName(BatchDownloadUnit firstUnit) {
  final dateParts = firstUnit.date.split('/');
  final formattedDate = '${dateParts[2]}_${dateParts[1]}_${dateParts[0]}';

  final employeeSegment =
      firstUnit.employeeName.trim().replaceAll(' ', '_').toUpperCase();
  final documentSegment =
      firstUnit.documentTemplateName.trim().replaceAll(' ', '_').toUpperCase();

  final id = firstUnit.documentUnitId;
  final idSuffix =
      id.length >= 4 ? id.substring(id.length - 4).toUpperCase() : id.toUpperCase();

  return '$formattedDate-$employeeSegment-$documentSegment-$idSuffix.PDF';
}
