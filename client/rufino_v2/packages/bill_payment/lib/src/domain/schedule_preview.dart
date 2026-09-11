/// The server's answer to "if I authorize for this date, when does the money
/// actually move?".
///
/// Mirrors `GET /bills/{id}/schedule-preview` — the same policy the submission
/// queue applies (banking calendar, working-day slide), computed by the server
/// so the sheet never re-implements it. Purely informative: the approval never
/// depends on this being available.
class SchedulePreview {
  /// Creates the preview.
  const SchedulePreview({
    required this.requestedDate,
    required this.effectiveDate,
    required this.slid,
    required this.immediate,
    required this.afterDueDate,
    this.available = true,
    this.unavailableReason,
  });

  /// The date the user asked to pay on.
  final DateTime requestedDate;

  /// The date the provider will actually execute the payment.
  final DateTime effectiveDate;

  /// Whether [effectiveDate] slid away from [requestedDate] (provider floor,
  /// weekend or banking holiday).
  final bool slid;

  /// Whether the server classified this as an immediate execution — an
  /// overdue bill pays at once, with no reaction window (ADR-017).
  final bool immediate;

  /// Whether [effectiveDate] falls after the bill's due date, so the payment
  /// leaves late and may carry interest. A warning, never a block.
  final bool afterDueDate;

  /// Whether this date can be scheduled right now.
  ///
  /// False only for TODAY outside the submission window — the same refusal the
  /// server gives on write (`BLP.BIL40`), said before anyone tries. The free
  /// date picker reaches today like any other day, so the suggestion
  /// disappearing from the sheet is not what keeps someone from choosing it.
  ///
  /// Defaults to true so an older server, which does not send the field, keeps
  /// behaving as it did: the write still refuses, just later.
  final bool available;

  /// Why it cannot be picked — a code from [ScheduleUnavailableReasons].
  final String? unavailableReason;
}

/// One of the four ready-made dates the scheduling sheet offers (ADR-021).
///
/// The names are the server's — they travel as strings in
/// `GET /bills/{id}/schedule-options`, and an unknown one degrades to
/// [ScheduleOptionKind.unknown] instead of throwing: a server that grows a
/// fifth suggestion must not break a client that has not shipped yet.
enum ScheduleOptionKind {
  /// Today. The only suggestion that depends on the time of day.
  today('Today'),

  /// Tomorrow.
  tomorrow('Tomorrow'),

  /// The day before the due date.
  dayBeforeDue('DayBeforeDue'),

  /// The due date itself.
  onDueDate('OnDueDate'),

  /// A suggestion this build does not know about.
  unknown('');

  const ScheduleOptionKind(this.wireName);

  /// The value the API uses for this option.
  final String wireName;

  /// Parses the API's value, degrading to [unknown] rather than throwing.
  static ScheduleOptionKind fromWire(String? value) {
    for (final kind in ScheduleOptionKind.values) {
      if (kind != ScheduleOptionKind.unknown && kind.wireName == value) {
        return kind;
      }
    }
    return ScheduleOptionKind.unknown;
  }
}

/// One suggestion, already resolved by the server.
///
/// The date arithmetic — "a day before the due date", "is today still on the
/// table?" — belongs to the server: it needs today in the policy's timezone,
/// the banking calendar and the provider's floor, none of which the client
/// has. The sheet renders what comes back and sends [date] as `scheduleFor`.
class ScheduleOptionPreview {
  /// Creates the suggestion.
  const ScheduleOptionPreview({
    required this.kind,
    required this.available,
    this.date,
    this.preview,
    this.unavailableReason,
  });

  /// Which suggestion this is.
  final ScheduleOptionKind kind;

  /// Whether it can be picked right now.
  final bool available;

  /// The date to send as `scheduleFor`; null when unavailable.
  final DateTime? date;

  /// What would happen for [date]; null when unavailable.
  final SchedulePreview? preview;

  /// Why it cannot be picked — a code, translated by the UI.
  final String? unavailableReason;
}

/// Why a suggestion is unavailable, in the API's own codes.
abstract final class ScheduleUnavailableReasons {
  /// Outside the submission window, so no payment can leave today.
  static const outsideWindow = 'outside_window';

  /// Today is not a banking day.
  static const notAWorkingDay = 'not_a_working_day';

  /// Today is before the first date the provider accepts.
  static const providerMinimum = 'provider_minimum';

  /// The bill has no known due date to anchor the suggestion on.
  static const noDueDate = 'no_due_date';

  /// The date this suggestion produces has already passed.
  static const inThePast = 'in_the_past';

  /// The sentence the sheet shows for [code].
  ///
  /// An unknown code still yields a sentence: a suggestion that is greyed out
  /// with no explanation reads as a bug, and the server may learn new codes
  /// before this build does.
  static String label(String? code) => switch (code) {
        outsideWindow => 'fora do horário de envio dos pagamentos',
        notAWorkingDay => 'hoje não é dia útil',
        providerMinimum => 'o provedor ainda não aceita esta data',
        noDueDate => 'este boleto não tem vencimento conhecido',
        inThePast => 'essa data já passou',
        _ => 'indisponível agora',
      };
}
