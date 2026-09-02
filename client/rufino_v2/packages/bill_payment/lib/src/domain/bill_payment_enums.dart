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

  /// Handed to the payment provider — the order was accepted (phase 3).
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

  /// Whether the reopen action applies — only a failed payment reopens.
  static bool acceptsReopen(String status) => status == failed;

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

/// Wire values of the backend's `PaymentOrderStatus` smart enum (phase 3).
///
/// `Draft` is ours (the submission queue's state); from `Pending` onwards the
/// provider dictates, and the bill mirrors the outcome.
abstract final class PaymentOrderStatuses {
  /// Created here, not yet submitted to the provider.
  static const String draft = 'Draft';

  /// The provider accepted and will process on the date.
  static const String pending = 'Pending';

  /// In bank processing.
  static const String bankProcessing = 'BankProcessing';

  /// Paid. Only a refund comes after.
  static const String paid = 'Paid';

  /// The provider could not pay, or the submission gave up. Terminal — a new
  /// try is a new order, born from reopening the bill.
  static const String failed = 'Failed';

  /// Cancelled before execution.
  static const String cancelled = 'Cancelled';

  /// The money came back after being paid.
  static const String refunded = 'Refunded';

  /// Whether the cancel action still applies — the reaction window the 24h
  /// policy exists for. Mirrors the server: draft cancels locally, pending and
  /// bank-processing ask the provider, and the rest is already settled.
  static bool canCancel(String status) =>
      status == draft || status == pending || status == bankProcessing;

  /// The label to show for [status]. Unknown values echo the wire name — a
  /// newer server must never be painted as a known outcome.
  static String label(String status) => switch (status) {
        draft => 'Na fila de envio',
        pending => 'Aceito pelo provedor',
        bankProcessing => 'Em processamento bancário',
        paid => 'Pago',
        failed => 'Falhou',
        cancelled => 'Cancelado',
        refunded => 'Estornado',
        _ => status,
      };
}

/// Wire values of the backend's `PaymentOrderHold` smart enum (phase 3).
///
/// A held order is a VISIBLE state, never a silently stuck queue: without a
/// payment account it waits for the key; an overdue bill waits for a person
/// to confirm paying right now.
abstract final class PaymentOrderHolds {
  /// No hold — the order is eligible for the submission queue.
  static const String none = 'None';

  /// The tenant has no payment account linked; linking the key releases it.
  static const String awaitingAccount = 'AwaitingAccount';

  /// The bill went overdue before submission — a person must confirm the
  /// immediate payment (ADR-017 on the server).
  static const String awaitingConfirmation = 'AwaitingConfirmation';

  /// The label to show for [hold]; `null` for [none] — no hold, no line.
  static String? label(String hold) => switch (hold) {
        none => null,
        awaitingAccount => 'Aguardando a conta de pagamento',
        awaitingConfirmation => 'Aguardando confirmação de pagamento imediato',
        _ => hold,
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

  /// Waiting its turn in the AI extractor's queue — the slow lane.
  static const String visionPending = 'VisionPending';

  /// Processing blew up and gave up retrying. Reopenable.
  ///
  /// Describes the system, not the document: [unrecognized] means "no bill in
  /// here", this one means the reading never finished. The distinction decides
  /// what the person does next — type the line by hand, or report a defect.
  static const String failed = 'Failed';

  /// A person looked at it and said they do not recognise the charge.
  ///
  /// Not [discarded]: that one is the system spotting a duplicate. This is a
  /// human decision, with an author recorded, and it is reversible — which is
  /// why the confirmation dialog says so.
  static const String dismissed = 'Dismissed';

  /// Whether the claim action applies to [status].
  static bool acceptsClaim(String status) => status == unrouted;

  /// Whether the reprocess action applies to [status].
  ///
  /// [failed] is included because a processing failure is exactly what a code
  /// fix tends to resolve — leaving it stuck would freeze the item on a verdict
  /// the current system would no longer give.
  static bool acceptsReprocess(String status) =>
      status == unrecognized ||
      status == locked ||
      status == linkFailed ||
      status == failed ||
      // Reabrir é como se desfaz uma reprovação — não há endpoint separado, porque
      // desfazer e reavaliar são a mesma operação: devolver o item à fila.
      status == dismissed;

  /// Whether the dismiss action applies to [status].
  ///
  /// Only the states still waiting on a person. A bill already promoted has money
  /// at stake, and one already routed to another payer is not this tenant's call.
  static bool acceptsDismiss(String status) =>
      status == unrecognized ||
      status == locked ||
      status == linkFailed ||
      status == failed ||
      status == unrouted;

  /// Whether [status] still counts as work waiting to be reviewed.
  static bool isPending(String status) =>
      status == received ||
      status == visionPending ||
      status == linkPending ||
      status == parsed ||
      acceptsDismiss(status);

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
        visionPending => 'Na fila da leitura por IA',
        failed => 'Falha no processamento',
        dismissed => 'Reprovado',
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

  /// A failure by the tenant's own declaration (blacklist, blocked origin)
  /// — one step above [blocking]: it turns the bill extreme danger.
  static const String critical = 'Critical';
}

/// Wire values of the backend's `RiskLevel` smart enum — the bill's flag.
///
/// The flag measures the worst evidence found: green when everything was
/// verified and matches, attention when nothing contradicts but verification
/// is incomplete, danger when sources contradict each other (or the official
/// lookup could not run), extreme danger when the tenant itself declared the
/// actor hostile (blacklisted payee, blocked origin).
abstract final class RiskLevels {
  /// Everything verified and matching.
  static const String safe = 'Safe';

  /// Nothing contradicts, but verification is incomplete.
  static const String attention = 'Attention';

  /// Sources contradict each other, or the central check could not run.
  static const String danger = 'Danger';

  /// The tenant declared the actor hostile — blacklist or blocked origin.
  static const String extremeDanger = 'ExtremeDanger';

  /// The ordered scale. Unknown values map to 0 — a newer server must never
  /// read as "safe by default".
  static int tier(String? level) => switch (level) {
        safe => 1,
        attention => 2,
        danger => 3,
        extremeDanger => 4,
        _ => 0,
      };

  /// Whether approving at this level requires the explicit "assumo o risco".
  static bool requiresAcknowledgement(String? level) =>
      level == danger || level == extremeDanger;

  /// PT label for the flag; unknown values echo raw so the screen never lies.
  static String label(String? level) => switch (level) {
        safe => 'Seguro',
        attention => 'Atenção',
        danger => 'Perigo',
        extremeDanger => 'Extremo Perigo',
        _ => level ?? '',
      };
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

/// Wire values of the backend's `ArtifactOutcome` smart enum — o que a captura
/// decidiu sobre um anexo.
///
/// Tem um valor que o `CaptureItemStatus` não tem e não poderia ter:
/// [discarded]. Artefato descartado não deixa item — a linha é apagada —, e é
/// justamente esse o caso em que uma pessoa fica sem saber o que houve com o
/// e-mail que mandou.
abstract final class ArtifactOutcomes {
  /// Ainda não processado, ou em trânsito pelo funil.
  static const String pending = 'Pending';

  /// Virou boleto deste cliente.
  static const String promoted = 'Promoted';

  /// Boleto sem dono determinado — fila de reivindicação.
  static const String unrouted = 'Unrouted';

  /// O documento diz, sob rótulo, que o pagador é outro.
  static const String foreignPayer = 'ForeignPayer';

  /// Nenhum boleto reconhecido, remetente cadastrado — ficou para revisão.
  static const String quarantined = 'Quarantined';

  /// PDF que nenhuma senha derivada abriu.
  static const String locked = 'Locked';

  /// O provedor não entregou o arquivo.
  static const String downloadFailed = 'DownloadFailed';

  /// Não era boleto e o remetente não é cadastrado: sumiu sem deixar item.
  static const String discarded = 'Discarded';

  /// O processamento estourou e desistiu — não se chegou a saber o que era.
  ///
  /// Não se confunde com [downloadFailed], onde o arquivo não veio: aqui ele
  /// veio e a leitura é que não fechou. Nem com [quarantined], que é resposta
  /// sobre o documento; esta é resposta sobre o sistema. Antes de existir, o
  /// anexo nessa situação ficava em [pending] para sempre.
  static const String processingFailed = 'ProcessingFailed';

  /// A mensagem não trouxe nada para processar — nem anexo, nem sinal no corpo.
  ///
  /// É o único valor que nunca aparece num anexo: ele descreve a mensagem, e é
  /// calculado justamente porque não há anexo de onde derivar desfecho. Existe
  /// porque a ausência estava sendo mostrada como [pending] — "Na fila" — para
  /// e-mails que não estavam em fila nenhuma.
  static const String nothingToProcess = 'NothingToProcess';

  /// The filters the screen offers, in reading order.
  static const List<String> filters = [
    promoted,
    unrouted,
    quarantined,
    downloadFailed,
    processingFailed,
    discarded,
    nothingToProcess,
  ];

  /// Whether the outcome asks someone to do something.
  static bool needsAttention(String outcome) =>
      outcome == unrouted || outcome == downloadFailed || outcome == locked;

  /// The label to show for [outcome].
  static String label(String outcome) => switch (outcome) {
        pending => 'Na fila',
        promoted => 'Virou boleto',
        unrouted => 'Aguardando reivindicação',
        foreignPayer => 'De outro pagador',
        quarantined => 'Não reconhecido',
        locked => 'Protegido por senha',
        downloadFailed => 'Download falhou',
        discarded => 'Descartado',
        processingFailed => 'Falha no processamento',
        nothingToProcess => 'Sem documento',
        _ => outcome,
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

/// Where the AI reading of a bill stands.
///
/// Mirrors the server's `ReadingStatus`. The bill is never blocked by it — the
/// queue is only about the analysis.
abstract final class ReadingStatuses {
  /// Nothing to read: hand-imported bill, unsupported media, extractor off.
  static const String notApplicable = 'NotApplicable';

  /// Waiting its turn in the queue.
  static const String queued = 'Queued';

  /// The reading was attached.
  static const String done = 'Done';

  /// Gave up after the retries, or the provider refused the artifact.
  static const String unavailable = 'Unavailable';

  /// What the user reads for each state — empty when there is nothing to say.
  ///
  /// [notApplicable] and [done] say nothing on purpose: the first is an
  /// absence the user cannot act on, and the second speaks through the fields
  /// it filled in.
  static String label(String status) => switch (status) {
        queued => 'Na fila para consulta com IA',
        unavailable => 'Consulta com IA indisponível',
        _ => '',
      };

  /// The one-line explanation that goes under [label].
  static String detail(String status) => switch (status) {
        queued =>
          'A competência, a descrição e a conferência do documento contra a '
              'consulta oficial chegam quando a leitura terminar. O boleto não '
              'espera por ela — pode ser aprovado assim mesmo.',
        unavailable =>
          'O extrator não conseguiu ler este documento depois das tentativas. '
              'A verificação "Documento × consulta oficial" fica sem base de '
              'comparação.',
        _ => '',
      };

  /// Whether this state is worth a line on the screen at all.
  static bool speaks(String status) => label(status).isNotEmpty;
}
