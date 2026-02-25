import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/domain/model/document/period.dart';

class PeriodBadgeComponent extends StatelessWidget {
  final Period? period;

  const PeriodBadgeComponent({required this.period, super.key});

  @override
  Widget build(BuildContext context) {
    if (period == null) {
      return const SizedBox.shrink();
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            _getIconForPeriodType(period!.type),
            size: 14,
          ),
          const SizedBox(width: 4),
          Text(
            "Competência: ",
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.bold,
            ),
          ),
          Text(
            period!.formattedPeriod,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }

  IconData _getIconForPeriodType(PeriodType type) {
    if (type.isDaily) return Icons.today;
    if (type.isWeekly) return Icons.view_week;
    if (type.isMonthly) return Icons.calendar_month;
    if (type.isYearly) return Icons.calendar_today;
    return Icons.date_range;
  }
}

class UsePreviousPeriodBadge extends StatelessWidget {
  final bool usePreviousPeriod;

  const UsePreviousPeriodBadge({
    required this.usePreviousPeriod,
    super.key,
  });

  @override
  Widget build(BuildContext context) {
    if (!usePreviousPeriod) {
      return const SizedBox.shrink();
    }

    return Container();
  }
}

class PeriodDetailComponent extends StatelessWidget {
  final Period period;

  const PeriodDetailComponent({required this.period, super.key});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  Icons.date_range,
                  size: 18,
                  color: Theme.of(context).colorScheme.primary,
                ),
                const SizedBox(width: 8),
                Text(
                  "Competência",
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: Theme.of(context).colorScheme.primary,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            _buildInfoRow("Tipo", period.type.name),
            _buildInfoRow("Período", period.formattedPeriod),
            if (period.day != null) _buildInfoRow("Dia", period.day.toString()),
            if (period.week != null)
              _buildInfoRow("Semana", period.week.toString()),
            if (period.month != null)
              _buildInfoRow("Mês", period.month.toString()),
            _buildInfoRow("Ano", period.year.toString()),
          ],
        ),
      ),
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          SizedBox(
            width: 80,
            child: Text(
              "$label:",
              style: const TextStyle(
                fontWeight: FontWeight.w500,
                fontSize: 13,
              ),
            ),
          ),
          Text(
            value,
            style: const TextStyle(fontSize: 13),
          ),
        ],
      ),
    );
  }
}
