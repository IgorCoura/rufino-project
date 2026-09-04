namespace BillPayment.Domain.Lookups;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Retrato do decode de um QR Pix (<c>POST /v3/pix/qrCodes/decode</c>). Imutável, como o
/// retrato do boleto.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É aqui que o CPF/CNPJ do recebedor aparece.</strong> O BR Code não carrega documento
/// nenhum — só chave e nome. Sem este decode, o trilho Pix não tem como cotejar beneficiário, e
/// é por isso que o check de consistência QR × código de barras precisa da consulta dos dois
/// lados, não só de um.
/// </para>
/// <para>
/// <strong><see cref="CanBePaid"/> é porteira, não evidência.</strong> QR que o provedor já sabe
/// que não paga não deve consumir verificação nem chegar à tela de aprovação — reprovar depois
/// de um humano aprovar seria gastar a atenção dele com um documento morto.
/// </para>
/// </remarks>
public sealed class PixLookupSnapshot : ValueObject
{
    public LookupParty Receiver { get; private set; } = default!;

    /// <summary>
    /// ISPB de 8 dígitos da instituição do recebedor. O trilho Pix identifica banco assim; o
    /// código de barras usa três dígitos. A tradução é de <c>IBankDirectory.FromIspb</c>.
    /// </summary>
    public string? ReceiverIspb { get; private set; }

    public string? ReceiverIspbName { get; private set; }

    /// <summary>PF ou PJ do recebedor segundo o provedor — precisa bater com o tipo do <c>Payee</c>.</summary>
    public TaxIdKind? ReceiverKind { get; private set; }

    /// <summary>Valor nominal do QR. Nulo em QR estático, que não carrega valor.</summary>
    public Money? Amount { get; private set; }

    /// <summary>Valor final, já com encargos e desconto. É este que o check de valor compara.</summary>
    public Money? TotalAmount { get; private set; }

    public Money? Interest { get; private set; }
    public Money? Fine { get; private set; }
    public Money? Discount { get; private set; }

    /// <summary>Pix Troco. Fora de escopo, registrado porque a presença muda o valor debitado.</summary>
    public Money? ChangeAmount { get; private set; }

    public DateOnly? DueDate { get; private set; }

    /// <summary>Prazo em que o QR deixa de ser pagável. Não tem equivalente no boleto.</summary>
    public DateTimeOffset? ExpirationDate { get; private set; }

    /// <summary>Valor aberto — o check de valor sai <c>Skipped</c>.</summary>
    public bool CanBePaidWithDifferentValue { get; private set; }

    /// <summary>Porteira anterior a tudo: o provedor recusa este QR de saída?</summary>
    public bool CanBePaid { get; private set; }

    public string? CannotBePaidReason { get; private set; }

    /// <summary>QR estático é reutilizável e traz menos campos; dinâmico é de uso único.</summary>
    public bool IsDynamic { get; private set; }

    public string? ConciliationIdentifier { get; private set; }

    /// <summary>Do que se trata a cobrança, segundo o emissor. Evidência para o aprovador.</summary>
    public string? Description { get; private set; }

    /// <summary>Pagador com documento mascarado. Só serve para contradizer — ver <see cref="MaskedParty"/>.</summary>
    public MaskedParty? Payer { get; private set; }

    public DateTimeOffset ConsultedAt { get; private set; }

    private PixLookupSnapshot() { }

    public static PixLookupSnapshot Create(
        LookupParty receiver,
        DateTimeOffset consultedAt,
        bool canBePaid = true,
        string? cannotBePaidReason = null,
        bool isDynamic = false,
        string? receiverIspb = null,
        string? receiverIspbName = null,
        TaxIdKind? receiverKind = null,
        Money? amount = null,
        Money? totalAmount = null,
        Money? interest = null,
        Money? fine = null,
        Money? discount = null,
        Money? changeAmount = null,
        DateOnly? dueDate = null,
        DateTimeOffset? expirationDate = null,
        bool canBePaidWithDifferentValue = false,
        string? conciliationIdentifier = null,
        MaskedParty? payer = null,
        string? description = null)
    {
        if (receiver is null)
            throw LookupErrors.BeneficiaryRequired();
        if (consultedAt == default)
            throw LookupErrors.ConsultedAtRequired();

        return new PixLookupSnapshot
        {
            Receiver = receiver,
            ReceiverIspb = string.IsNullOrWhiteSpace(receiverIspb) ? null : receiverIspb.Trim(),
            ReceiverIspbName = string.IsNullOrWhiteSpace(receiverIspbName) ? null : receiverIspbName.Trim(),
            ReceiverKind = receiverKind,
            Amount = amount,
            TotalAmount = totalAmount,
            Interest = interest,
            Fine = fine,
            Discount = discount,
            ChangeAmount = changeAmount,
            DueDate = dueDate,
            ExpirationDate = expirationDate,
            CanBePaidWithDifferentValue = canBePaidWithDifferentValue,
            CanBePaid = canBePaid,
            CannotBePaidReason = string.IsNullOrWhiteSpace(cannotBePaidReason) ? null : cannotBePaidReason.Trim(),
            IsDynamic = isDynamic,
            ConciliationIdentifier = string.IsNullOrWhiteSpace(conciliationIdentifier) ? null : conciliationIdentifier.Trim(),
            Payer = payer,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ConsultedAt = consultedAt,
        };
    }

    /// <summary>O valor que o check de valor deve olhar: o total com encargos, se houver.</summary>
    public Money? PayableAmount => TotalAmount ?? Amount;

    public bool SupportsAmountCheck => PayableAmount is not null && !CanBePaidWithDifferentValue;

    /// <summary>Já passou do prazo de pagamento do QR no instante informado?</summary>
    public bool IsExpiredAt(DateTimeOffset instant) => ExpirationDate is not null && instant > ExpirationDate.Value;

    public TimeSpan AgeAt(DateTimeOffset instant) => instant - ConsultedAt;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Receiver;
        yield return ReceiverIspb;
        yield return ReceiverIspbName;
        yield return ReceiverKind;
        yield return Amount;
        yield return TotalAmount;
        yield return Interest;
        yield return Fine;
        yield return Discount;
        yield return ChangeAmount;
        yield return DueDate;
        yield return ExpirationDate;
        yield return CanBePaidWithDifferentValue;
        yield return CanBePaid;
        yield return CannotBePaidReason;
        yield return IsDynamic;
        yield return ConciliationIdentifier;
        yield return Payer;
        yield return Description;
        yield return ConsultedAt;
    }
}
