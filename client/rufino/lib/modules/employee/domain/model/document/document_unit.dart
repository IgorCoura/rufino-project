import 'package:equatable/equatable.dart';
import 'package:intl/intl.dart';
import 'package:rufino/modules/employee/domain/model/document/document_unit_status.dart';
import 'package:rufino/modules/employee/domain/model/document/period.dart';

class DocumentUnit extends Equatable {
  final String id;
  final String content;
  final String validity;
  final String name;
  final String extension;
  final DocumentUnitStatus status;
  final String date;
  final Period? period;
  final String createAt;
  final String updateAt;

  const DocumentUnit(
      this.id,
      this.content,
      this.validity,
      this.name,
      this.extension,
      this.status,
      this.date,
      this.period,
      this.createAt,
      this.updateAt);

  const DocumentUnit.empty()
      : id = "",
        content = "",
        validity = "",
        name = "",
        extension = "",
        status = const DocumentUnitStatus.empty(),
        date = "",
        period = null,
        createAt = "",
        updateAt = "";

  factory DocumentUnit.fromJson(Map<String, dynamic> json) {
    return DocumentUnit(
      json['id'] as String,
      json['content'] as String,
      json['validity'] ?? "",
      json['name'] ?? "",
      json['extension'] ?? "",
      DocumentUnitStatus.fromJson(json['status'] as Map<String, dynamic>),
      json['date'] as String,
      json['period'] != null
          ? Period.fromJson(json['period'] as Map<String, dynamic>)
          : null,
      json['createAt'] as String,
      json['updateAt'] as String,
    );
  }

  static List<DocumentUnit> fromJsonList(List<dynamic> jsonList) {
    return jsonList
        .map((json) => DocumentUnit.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  bool get isPanding => status.isPanding;
  bool get hasFile => getDate.isEmpty == false && extension.isEmpty == false;

  String get getDate {
    if (date.isEmpty || date == "0001-01-01") {
      return "__/__/____ ";
    }
    final dateTime = DateTime.parse(date);
    return "${dateTime.day.toString().padLeft(2, '0')}/"
        "${dateTime.month.toString().padLeft(2, '0')}/"
        "${dateTime.year} ";
  }

  String get getValidity {
    if (validity.isEmpty) {
      return "__/__/____ ";
    }
    final dateTime = DateTime.parse(validity);
    return "${dateTime.day.toString().padLeft(2, '0')}/"
        "${dateTime.month.toString().padLeft(2, '0')}/"
        "${dateTime.year} ";
  }

  String get getCreateAt {
    final dateTime = DateTime.parse(createAt);
    return "${dateTime.day.toString().padLeft(2, '0')}/"
        "${dateTime.month.toString().padLeft(2, '0')}/"
        "${dateTime.year} "
        "${dateTime.hour.toString().padLeft(2, '0')}:"
        "${dateTime.minute.toString().padLeft(2, '0')}:"
        "${dateTime.second.toString().padLeft(2, '0')}";
  }

  static String? validateDate(String? value) {
    if (value == null || value.isEmpty) {
      return "A data não pode ser vazio.";
    }

    try {
      // Parse the input date string in dd/MM/yyyy format
      DateTime parsedDate = DateFormat('dd/MM/yyyy').parse(value);
      // Format the parsed date to yyyy-MM-dd
      String formattedDate = DateFormat('yyyy-MM-dd').format(parsedDate);

      var date = DateTime.tryParse(formattedDate);

      var dateMax = DateTime.now().add(const Duration(days: 730));
      var dateMin = DateTime.now().add(const Duration(days: -730));

      if (date == null || date.isAfter(dateMax) || date.isBefore(dateMin)) {
        return "A data é invalida.";
      }
    } catch (_) {
      return "A data é invalida.";
    }
    return null;
  }

  static String? validateDateLimitToSign(String? value) {
    if (value == null || value.isEmpty) {
      return "A data não pode ser vazio.";
    }

    try {
      // Parse the input date string in dd/MM/yyyy format
      DateTime parsedDate = DateFormat('dd/MM/yyyy').parse(value);
      // Format the parsed date to yyyy-MM-dd
      String formattedDate = DateFormat('yyyy-MM-dd').format(parsedDate);

      var date = DateTime.tryParse(formattedDate);

      var dateMax = DateTime.now().add(const Duration(days: 1460));
      var dateMin = DateTime.now();

      if (date == null || date.isAfter(dateMax) || date.isBefore(dateMin)) {
        return "A data é invalida.";
      }
    } catch (_) {
      return "A data é invalida.";
    }
    return null;
  }

  static String? validateEminderEveryNDays(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    try {
      var parsedValue = int.tryParse(value);

      var max = 90;
      var min = 0;

      if (parsedValue == null || parsedValue > max || parsedValue < min) {
        return "O valor é invalida.";
      }
    } catch (_) {
      return "O valor  é invalida.";
    }
    return null;
  }

  bool get hasPeriod => period != null;

  String get getPeriodFormatted {
    if (period == null) return "";
    return period!.formattedPeriod;
  }

  @override
  List<Object?> get props => [
        id,
        content,
        validity,
        name,
        extension,
        status,
        date,
        period,
        createAt,
        updateAt,
      ];
}
