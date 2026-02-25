import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';

class PeriodType extends Enumeration {
  static const Map<String, String> conversionMapIntToString = {
    "1": "Diário",
    "2": "Semanal",
    "3": "Mensal",
    "4": "Anual",
  };

  PeriodType(String id, String name)
      : super(id, conversionMapIntToString[id] ?? name, "Tipo de Período");

  const PeriodType.empty() : super.empty(displayName: "Tipo de Período");

  bool get isDaily => id == "1";
  bool get isWeekly => id == "2";
  bool get isMonthly => id == "3";
  bool get isYearly => id == "4";

  static PeriodType fromJson(Map<String, dynamic> json) {
    return PeriodType((json["id"]).toString(), json["name"]);
  }
}

class Period extends Equatable {
  final PeriodType type;
  final int? day;
  final int? week;
  final int? month;
  final int year;

  const Period({
    required this.type,
    this.day,
    this.week,
    this.month,
    required this.year,
  });

  const Period.empty()
      : type = const PeriodType.empty(),
        day = null,
        week = null,
        month = null,
        year = 0;

  factory Period.fromJson(Map<String, dynamic> json) {
    return Period(
      type: PeriodType.fromJson(json['type'] as Map<String, dynamic>),
      day: json['day'] as int?,
      week: json['week'] as int?,
      month: json['month'] as int?,
      year: json['year'] as int,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'type': {'id': type.id, 'name': type.name},
      'day': day,
      'week': week,
      'month': month,
      'year': year,
    };
  }

  String get formattedPeriod {
    if (type.isDaily) {
      return "${day.toString().padLeft(2, '0')}/${month.toString().padLeft(2, '0')}/$year";
    } else if (type.isWeekly) {
      return "Semana $week - ${month.toString().padLeft(2, '0')}/$year";
    } else if (type.isMonthly) {
      return "${_monthName(month)}/$year";
    } else if (type.isYearly) {
      return "$year";
    }
    return "";
  }

  String _monthName(int? month) {
    const months = [
      "Jan",
      "Fev",
      "Mar",
      "Abr",
      "Mai",
      "Jun",
      "Jul",
      "Ago",
      "Set",
      "Out",
      "Nov",
      "Dez",
    ];
    if (month != null && month >= 1 && month <= 12) {
      return months[month - 1];
    }
    return month?.toString().padLeft(2, '0') ?? "--";
  }

  @override
  List<Object?> get props => [type, day, week, month, year];
}
