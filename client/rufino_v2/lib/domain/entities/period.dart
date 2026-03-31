/// A competency period associated with a document unit.
///
/// Period types: Daily (1), Weekly (2), Monthly (3), Yearly (4).
/// Fields are populated according to the type — for example, [day] is
/// only non-null for daily periods.
class Period {
  const Period({
    required this.typeId,
    required this.typeName,
    this.day,
    this.week,
    required this.month,
    required this.year,
  });

  /// The period type identifier: 1=Daily, 2=Weekly, 3=Monthly, 4=Yearly.
  final int typeId;

  /// The period type display name.
  final String typeName;

  /// The day of month, populated only for daily periods.
  final int? day;

  /// The week number, populated only for weekly periods.
  final int? week;

  /// The month (1–12).
  final int month;

  /// The year.
  final int year;

  /// Whether this is a daily period.
  bool get isDaily => typeId == 1;

  /// Whether this is a weekly period.
  bool get isWeekly => typeId == 2;

  /// Whether this is a monthly period.
  bool get isMonthly => typeId == 3;

  /// Whether this is a yearly period.
  bool get isYearly => typeId == 4;

  /// Human-readable period description.
  String get formattedPeriod {
    if (isDaily && day != null) {
      return '${day.toString().padLeft(2, '0')}/${month.toString().padLeft(2, '0')}/$year';
    } else if (isWeekly && week != null) {
      return 'Semana $week - ${month.toString().padLeft(2, '0')}/$year';
    } else if (isMonthly) {
      return '${_monthName(month)}/$year';
    } else if (isYearly) {
      return '$year';
    }
    return '';
  }

  static String _monthName(int month) => switch (month) {
        1 => 'Jan',
        2 => 'Fev',
        3 => 'Mar',
        4 => 'Abr',
        5 => 'Mai',
        6 => 'Jun',
        7 => 'Jul',
        8 => 'Ago',
        9 => 'Set',
        10 => 'Out',
        11 => 'Nov',
        12 => 'Dez',
        _ => '',
      };
}
