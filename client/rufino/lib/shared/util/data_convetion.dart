import 'package:intl/intl.dart';

class DataConvetion {
  static DateTime convertToData(String date) {
    final parts = date.split('/');
    if (parts.length != 3) {
      throw FormatException('Invalid date format. Expected dd/mm/yyyy');
    }
    final day = int.parse(parts[0]);
    final month = int.parse(parts[1]);
    final year = int.parse(parts[2]);
    return DateTime(year, month, day);
  }

  static String convertToIsoData(String date) {
    final dateTime = convertToData(date);
    return dateTime.toIso8601String();
  }

  static String convertToDataOnly(String date) {
    final dateTime = convertToData(date);
    String formattedDate = DateFormat('yyyy-MM-dd').format(dateTime);
    return formattedDate;
  }

  static String convertToIsoUTC(String date) {
    final dateTime = convertToData(date);
    final utcDate = dateTime.toUtc();
    return utcDate.toIso8601String();
  }

  static String convertToLocalWithOffset(String date) {
    final now = convertToData(date);
    final offset = now.timeZoneOffset;

    final hours = offset.inHours.abs().toString().padLeft(2, '0');
    final minutes = (offset.inMinutes.abs() % 60).toString().padLeft(2, '0');
    final sign = offset.isNegative ? '-' : '+';

    return "${now.toIso8601String().split('.').first}$sign$hours:$minutes";
  }
}
