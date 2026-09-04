namespace BillPayment.Domain.Lookups;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Retrato da consulta oficial de um documento de código de barras (<c>POST /v3/bill/simulate</c>).
/// Imutável: uma nova consulta gera um novo retrato, nunca atualiza este.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Por que quase tudo é opcional.</strong> A medição da sprint 1.0 contra as 22 linhas
/// do corpus real mostrou que a cobertura varia por natureza do documento: arrecadação devolve
/// nome e valor em 100% dos casos, documento do beneficiário em 0% e vencimento em 30%. Um VO
/// que exigisse esses campos só conseguiria ser construído para uma fatia dos documentos, e o
/// adapter teria de inventar valor para o resto. O campo ausente é informação — vira check
/// <c>Inconclusive</c>, não check aprovado. Ver <c>12-official-lookup-coverage.md</c>.
/// </para>
/// <para>
/// <strong>O que este retrato não tem: pagador.</strong> O provedor não devolve nenhum campo de
/// pagador para código de barras. Essa ausência é a razão inteira do ADR-004 e do check de
/// pagador operar sobre o que o PDF trouxe, não sobre o que a consulta confirmou.
/// </para>
/// </remarks>
public sealed class LookupSnapshot : ValueObject
{
    public LookupParty Beneficiary { get; private set; } = default!;

    /// <summary>
    /// Banco liquidante segundo o provedor. Serve de <strong>conferência cruzada</strong> do
    /// que <c>DigitableLine.BankCode</c> já lê das posições 1–3; divergência entre os dois é
    /// bloqueante. Nulo em arrecadação, que não carrega banco em posição nenhuma.
    /// </summary>
    public BankCode? BankCode { get; private set; }

    /// <summary>Valor a pagar hoje, já com encargos e descontos aplicados pelo emissor.</summary>
    public Money? Amount { get; private set; }

    /// <summary>Valor de face, antes dos encargos. É o que explica a diferença para <see cref="Amount"/>.</summary>
    public Money? OriginalAmount { get; private set; }

    public Money? Interest { get; private set; }
    public Money? Fine { get; private set; }
    public Money? Discount { get; private set; }

    /// <summary>Piso aceito pelo emissor quando o valor é editável.</summary>
    public Money? MinAmount { get; private set; }

    /// <summary>Teto aceito pelo emissor quando o valor é editável.</summary>
    public Money? MaxAmount { get; private set; }

    /// <summary>Valor aberto — o check de valor sai <c>Skipped</c>, não aprovado.</summary>
    public bool AllowChangeValue { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public bool IsOverdue { get; private set; }

    /// <summary>Tarifa que o provedor cobra pela liquidação. Entra no relatório de custo.</summary>
    public Money? Fee { get; private set; }

    /// <summary>Piso de agendamento imposto pelo provedor. O agendamento da fase 3 não pode ignorá-lo.</summary>
    public DateOnly? MinimumScheduleDate { get; private set; }

    /// <summary>Quando o retrato foi tirado. É o que dá validade — e prazo de validade — à evidência.</summary>
    public DateTimeOffset ConsultedAt { get; private set; }

    private LookupSnapshot() { }

    public static LookupSnapshot Create(
        LookupParty beneficiary,
        DateTimeOffset consultedAt,
        BankCode? bankCode = null,
        Money? amount = null,
        Money? originalAmount = null,
        Money? interest = null,
        Money? fine = null,
        Money? discount = null,
        Money? minAmount = null,
        Money? maxAmount = null,
        bool allowChangeValue = false,
        DateOnly? dueDate = null,
        bool isOverdue = false,
        Money? fee = null,
        DateOnly? minimumScheduleDate = null)
    {
        if (beneficiary is null)
            throw LookupErrors.BeneficiaryRequired();
        if (consultedAt == default)
            throw LookupErrors.ConsultedAtRequired();
        if (minAmount is not null && maxAmount is not null && minAmount.Amount > maxAmount.Amount)
            throw LookupErrors.AmountBoundsInverted(minAmount.Amount, maxAmount.Amount);

        return new LookupSnapshot
        {
            Beneficiary = beneficiary,
            BankCode = bankCode,
            Amount = amount,
            OriginalAmount = originalAmount,
            Interest = interest,
            Fine = fine,
            Discount = discount,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            AllowChangeValue = allowChangeValue,
            DueDate = dueDate,
            IsOverdue = isOverdue,
            Fee = fee,
            MinimumScheduleDate = minimumScheduleDate,
            ConsultedAt = consultedAt,
        };
    }

    /// <summary>
    /// O check de valor tem base para decidir? Valor editável ou ausente não reprova nada.
    /// </summary>
    public bool SupportsAmountCheck => Amount is not null && !AllowChangeValue;

    /// <summary>Idade do retrato no instante informado — insumo da expiração de snapshot da sprint 1.5.</summary>
    public TimeSpan AgeAt(DateTimeOffset instant) => instant - ConsultedAt;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Beneficiary;
        yield return BankCode;
        yield return Amount;
        yield return OriginalAmount;
        yield return Interest;
        yield return Fine;
        yield return Discount;
        yield return MinAmount;
        yield return MaxAmount;
        yield return AllowChangeValue;
        yield return DueDate;
        yield return IsOverdue;
        yield return Fee;
        yield return MinimumScheduleDate;
        yield return ConsultedAt;
    }
}
