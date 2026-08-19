/// Wire values of the backend's smart enums.
///
/// The strings are the contract — they travel in the payload and come back in
/// the read model. The labels beside them are presentation, and only exist so
/// a screen never has to invent its own translation.
library;

/// Wire values of the backend's `BillStatus` smart enum.
///
/// The state machine lives on the server; what the UI needs from it is which
/// actions each status admits, mirrored here so buttons can hide themselves
/// without a round trip.
abstract final class BillStatuses {
  /// Just entered — instrument parsed, validation not run yet.
  static const String captured = 'Captured';

  /// Validated with no blocking failure; a human can decide.
  static const String awaitingApproval = 'AwaitingApproval';

  /// A blocking check failed. Revalidation is the way back.
  static const String rejected = 'Rejected';

  /// A human authorized the payment.
  static const String approved = 'Approved';

  /// A human refused it. Terminal.
  static const String denied = 'Denied';

  /// Handed to the payment provider (phase 3 — not produced yet).
  static const String scheduled = 'Scheduled';

  /// Paid. Terminal.
  static const String paid = 'Paid';

  /// The payment attempt failed; back to a human.
  static const String failed = 'Failed';

  /// Removed from the flow. Terminal.
  static const String cancelled = 'Cancelled';

  /// Whether [status] admits no further mutation.
  static bool isTerminal(String status) =>
      status == denied || status == paid || status == cancelled;

  /// Whether the revalidate action applies to [status].
  ///
  /// Mirrors the server's `AcceptsValidation`: from `Scheduled` onwards the
  /// button must disappear.
  static bool acceptsValidation(String status) =>
      status == captured ||
      status == rejected ||
      status == awaitingApproval ||
      status == approved;

  /// Whether approve/deny apply to [status].
  static bool acceptsDecision(String status) => status == awaitingApproval;

  /// Whether cancel applies to [status].
  static bool acceptsCancellation(String status) => !isTerminal(status);

  /// The label to show for [status].
  static String label(String status) => switch (status) {
        captured => 'Capturado',
        awaitingApproval => 'Aguardando aprovação',
        rejected => 'Rejeitado',
        approved => 'Aprovado',
        denied => 'Negado',
        scheduled => 'Agendado',
        paid => 'Pago',
        failed => 'Falhou',
        cancelled => 'Cancelado',
        _ => status,
      };
}

/// Wire values of the backend's `CaptureItemStatus` smart enum.
abstract final class CaptureItemStatuses {
  /// Ingested; nothing known about it yet.
  static const String received = 'Received';

  /// A valid instrument was extracted (passing state since routing exists).
  static const String parsed = 'Parsed';

  /// An encrypted PDF no derived password opened.
  static const String locked = 'Locked';

  /// There is a link to the document; download pending.
  static const String linkPending = 'LinkPending';

  /// The link ladder ran out.
  static const String linkFailed = 'LinkFailed';

  /// Routed to this tenant and became a bill. Terminal.
  static const String promoted = 'Promoted';

  /// The identified payer is not this tenant. Terminal, not claimable.
  static const String foreignPayer = 'ForeignPayer';

  /// No way to determine the owner — the claim queue.
  static const String unrouted = 'Unrouted';

  /// No valid instrument found in the artifact.
  static const String unrecognized = 'Unrecognized';

  /// Duplicate of an already processed artifact. Terminal.
  static const String discarded = 'Discarded';

  /// Whether the claim action applies to [status].
  static bool acceptsClaim(String status) => status == unrouted;

  /// Whether the reprocess action applies to [status].
  static bool acceptsReprocess(String status) =>
      status == unrecognized || status == locked || status == linkFailed;

  /// The label to show for [status].
  static String label(String status) => switch (status) {
        received => 'Recebido',
        parsed => 'Lido',
        locked => 'Protegido por senha',
        linkPending => 'Aguardando download',
        linkFailed => 'Download falhou',
        promoted => 'Virou boleto',
        foreignPayer => 'De outro pagador',
        unrouted => 'Aguardando reivindicação',
        unrecognized => 'Não reconhecido',
        discarded => 'Descartado',
        _ => status,
      };
}

/// Wire values of the backend's `CheckOutcome` smart enum — five, not three.
abstract final class CheckOutcomes {
  /// The check verified and agreed.
  static const String passed = 'Passed';

  /// The check verified and disagreed. The only outcome that blocks.
  static const String failed = 'Failed';

  /// There was not enough data to verify.
  static const String inconclusive = 'Inconclusive';

  /// The check does not apply to this document.
  static const String skipped = 'Skipped';

  /// Something is off but never blocks, whatever the severity.
  static const String warning = 'Warning';

  /// Whether [outcome] reproves the bill.
  static bool isFailure(String outcome) => outcome == failed;

  /// Whether [outcome] deserves the approver's eyes.
  static bool requiresAttention(String outcome) =>
      outcome == failed || outcome == inconclusive || outcome == warning;

  /// The label to show for [outcome].
  static String label(String outcome) => switch (outcome) {
        passed => 'Verificado',
        failed => 'Reprovado',
        inconclusive => 'Inconclusivo',
        skipped => 'Não se aplica',
        warning => 'Atenção',
        _ => outcome,
      };
}

/// Wire values of the backend's `CheckSeverity` smart enum.
abstract final class CheckSeverities {
  /// A failure here reproves the bill.
  static const String blocking = 'Blocking';

  /// A failure here informs, never blocks.
  static const String advisory = 'Advisory';
}

/// Wire values of the backend's `BillKind` smart enum.
abstract final class BillKinds {
  /// A registered bank slip (cobrança).
  static const String bankSlip = 'BankSlip';

  /// A utility/arrecadação document — carries no bank code.
  static const String utility = 'Utility';

  /// The label to show for [kind].
  static String label(String kind) =>
      kind == utility ? 'Arrecadação' : 'Boleto bancário';
}

/// Wire values of the backend's `PaymentRail` smart enum.
abstract final class PaymentRails {
  /// The preferred rail (ADR-010).
  static const String pix = 'Pix';

  /// The traditional rail.
  static const String boleto = 'Boleto';
}

/// Wire values of the backend's `BillSourceKind` smart enum.
abstract final class BillSourceKinds {
  /// Captured from a monitored mailbox.
  static const String mailbox = 'Mailbox';

  /// Captured from a portal (phase 5).
  static const String portal = 'Portal';

  /// Imported by hand.
  static const String manualUpload = 'ManualUpload';

  /// The label to show for [kind].
  static String label(String kind) => switch (kind) {
        mailbox => 'Caixa de e-mail',
        portal => 'Portal',
        manualUpload => 'Importação manual',
        _ => kind,
      };
}

/// Wire values of the backend's `RoutingConfidence` smart enum.
abstract final class RoutingConfidences {
  /// Conclusive: the tenant's own document proved ownership.
  static const String strong = 'Strong';

  /// Conclusive by learned linkage.
  static const String learned = 'Learned';

  /// Resolved by exclusive registration, not by proof.
  static const String weak = 'Weak';

  /// A human claimed it.
  static const String claimed = 'Claimed';

  /// The label to show for [confidence].
  static String label(String confidence) => switch (confidence) {
        strong => 'Forte',
        learned => 'Aprendida',
        weak => 'Fraca',
        claimed => 'Reivindicada',
        _ => confidence,
      };
}

/// Wire values of the backend's `ExtractionMethod` smart enum.
abstract final class ExtractionMethods {
  /// Read from the PDF's embedded text layer.
  static const String embeddedText = 'EmbeddedText';

  /// Read from a QR/ITF image.
  static const String qrCode = 'QrCode';

  /// Read by the vision extractor (spends daily quota).
  static const String vision = 'Vision';

  /// Typed by a person.
  static const String manual = 'Manual';

  /// Read from the e-mail body.
  static const String emailBody = 'EmailBody';

  /// The label to show for [method].
  static String label(String method) => switch (method) {
        embeddedText => 'Texto do PDF',
        qrCode => 'Código na imagem',
        vision => 'Leitura por IA',
        manual => 'Manual',
        emailBody => 'Corpo do e-mail',
        _ => method,
      };
}

/// Wire values of the backend's `Recurrence` smart enum.
abstract final class Recurrences {
  /// Every month.
  static const String monthly = 'Monthly';

  /// Every two months.
  static const String bimonthly = 'Bimonthly';

  /// Every three months.
  static const String quarterly = 'Quarterly';

  /// Once a year.
  static const String annual = 'Annual';

  /// Every value, in presentation order.
  static const List<String> all = [monthly, bimonthly, quarterly, annual];

  /// The label to show for [recurrence].
  static String label(String recurrence) => switch (recurrence) {
        monthly => 'Mensal',
        bimonthly => 'Bimestral',
        quarterly => 'Trimestral',
        annual => 'Anual',
        _ => recurrence,
      };
}

/// Wire values of the backend's `CycleStatus` smart enum.
abstract final class CycleStatuses {
  /// The bill has not arrived yet, and it is not late.
  static const String waiting = 'Waiting';

  /// The bill arrived and matched.
  static const String fulfilled = 'Fulfilled';

  /// Something arrived but could not be fully read.
  static const String partiallyCaptured = 'PartiallyCaptured';

  /// The bill did not arrive by the alert date.
  static const String missing = 'Missing';

  /// A person dismissed this cycle.
  static const String waived = 'Waived';

  /// The label to show for [status].
  static String label(String status) => switch (status) {
        waiting => 'Aguardando',
        fulfilled => 'Recebido',
        partiallyCaptured => 'Chegou com problema',
        missing => 'Não chegou',
        waived => 'Dispensado',
        _ => status,
      };
}

/// Wire values of the backend's `MissReason` smart enum.
abstract final class MissReasons {
  /// Nothing arrived at all.
  static const String neverArrived = 'NeverArrived';

  /// The portal was unreachable.
  static const String portalUnavailable = 'PortalUnavailable';

  /// Something arrived and extraction failed.
  static const String captureFailed = 'CaptureFailed';

  /// Something arrived locked by a password.
  static const String locked = 'Locked';

  /// Something arrived behind a link that failed.
  static const String linkFailed = 'LinkFailed';

  /// Something arrived and could not be routed.
  static const String unrouted = 'Unrouted';

  /// Whether [reason] means something DID arrive — the fix is on the item,
  /// not at the sender.
  static bool arrived(String reason) =>
      reason == captureFailed ||
      reason == locked ||
      reason == linkFailed ||
      reason == unrouted;

  /// The label to show for [reason].
  static String label(String reason) => switch (reason) {
        neverArrived => 'Nunca chegou',
        portalUnavailable => 'Portal indisponível',
        captureFailed => 'Falha na leitura',
        locked => 'Protegido por senha',
        linkFailed => 'Download falhou',
        unrouted => 'Sem dono definido',
        _ => reason,
      };
}

/// Wire values of the backend's `AlertLevel` smart enum.
abstract final class AlertLevels {
  /// First notice, at the alert date.
  static const String headsUp = 'HeadsUp';

  /// Three days before the due date.
  static const String warning = 'Warning';

  /// On the due date.
  static const String urgent = 'Urgent';

  /// Past the due date.
  static const String overdue = 'Overdue';

  /// The label to show for [level].
  static String label(String level) => switch (level) {
        headsUp => 'Aviso',
        warning => 'Atenção',
        urgent => 'Urgente',
        overdue => 'Vencido',
        _ => level,
      };
}

/// Wire values of the backend's `AmountPolicyKind` smart enum.
abstract final class AmountPolicyKinds {
  /// A fixed expected amount with a tolerance.
  static const String fixed = 'Fixed';

  /// A minimum/maximum range.
  static const String range = 'Range';

  /// Any amount — the amount check becomes inconclusive.
  static const String unbounded = 'Unbounded';

  /// The label to show for [kind].
  static String label(String kind) => switch (kind) {
        fixed => 'Valor fixo',
        range => 'Faixa de valores',
        unbounded => 'Sem limite',
        _ => kind,
      };
}

/// Wire values of the backend's `OriginKind` smart enum.
abstract final class OriginKinds {
  /// An exact e-mail address. Highest match precedence.
  static const String emailAddress = 'EmailAddress';

  /// An e-mail domain.
  static const String emailDomain = 'EmailDomain';

  /// A web domain.
  static const String webDomain = 'WebDomain';

  /// The label to show for [kind].
  static String label(String kind) => switch (kind) {
        emailAddress => 'Endereço de e-mail',
        emailDomain => 'Domínio de e-mail',
        webDomain => 'Domínio web',
        _ => kind,
      };
}

/// Wire values of the backend's `TrustDecision` smart enum.
abstract final class TrustDecisions {
  /// Bills from this origin are expected.
  static const String trusted = 'Trusted';

  /// Bills from this origin are refused.
  static const String blocked = 'Blocked';

  /// The label to show for [decision].
  static String label(String decision) =>
      decision == blocked ? 'Bloqueada' : 'Confiável';
}

/// Wire values of the backend's `MailboxStatus`, as returned by the sync
/// endpoint.
abstract final class SyncStatuses {
  /// The sync completed.
  static const String ok = 'Ok';

  /// The provider refused access — fix the app registration.
  static const String denied = 'Denied';

  /// The provider expired the cursor — a rescan is the way back.
  static const String cursorExpired = 'CursorExpired';

  /// The provider was unreachable — try again later.
  static const String unavailable = 'Unavailable';

  /// The label to show for [status].
  static String label(String status) => switch (status) {
        ok => 'Sincronizado',
        denied => 'Acesso negado',
        cursorExpired => 'Releitura necessária',
        unavailable => 'Indisponível',
        _ => status,
      };
}

/// Wire values of the backend's `ExpectationOrigin` smart enum.
abstract final class ExpectationOrigins {
  /// Learned from the bill history.
  static const String learned = 'Learned';

  /// Registered by a person.
  static const String manual = 'Manual';

  /// The label to show for [origin].
  static String label(String origin) =>
      origin == learned ? 'Aprendida' : 'Manual';
}
