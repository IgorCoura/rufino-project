import '../../domain/entities/period.dart';

/// API response model for a document unit period.
class PeriodApiModel {
  const PeriodApiModel({
    required this.typeId,
    required this.typeName,
    this.day,
    this.week,
    required this.month,
    required this.year,
  });

  final int typeId;
  final String typeName;
  final int? day;
  final int? week;
  final int month;
  final int year;

  /// Deserializes from the API JSON structure.
  factory PeriodApiModel.fromJson(Map<String, dynamic> json) {
    final typeJson = json['type'] as Map<String, dynamic>?;
    return PeriodApiModel(
      typeId: typeJson?['id'] as int? ?? 0,
      typeName: typeJson?['name'] as String? ?? '',
      day: json['day'] as int?,
      week: json['week'] as int?,
      month: json['month'] as int? ?? 0,
      year: json['year'] as int? ?? 0,
    );
  }

  /// Converts this DTO to a domain [Period] entity.
  Period toEntity() {
    return Period(
      typeId: typeId,
      typeName: typeName,
      day: day,
      week: week,
      month: month,
      year: year,
    );
  }
}
