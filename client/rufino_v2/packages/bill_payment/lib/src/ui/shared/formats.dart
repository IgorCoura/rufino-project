import 'package:intl/intl.dart';

final _money = NumberFormat.currency(locale: 'pt_BR', symbol: r'R$');
final _date = DateFormat('dd/MM/yyyy');
final _dateTime = DateFormat('dd/MM/yyyy HH:mm');

/// Formats [amount] as Brazilian currency, or a dash when absent.
String formatMoney(double? amount) =>
    amount == null ? '—' : _money.format(amount);

/// Formats [date] as `dd/MM/yyyy`, or a dash when absent.
String formatDate(DateTime? date) =>
    date == null ? '—' : _date.format(date.toLocal());

/// Formats [date] with time, or a dash when absent.
String formatDateTime(DateTime? date) =>
    date == null ? '—' : _dateTime.format(date.toLocal());
