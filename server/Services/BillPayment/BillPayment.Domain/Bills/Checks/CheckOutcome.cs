namespace BillPayment.Domain.Bills.Checks;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Como terminou uma verificação.
/// </summary>
/// <remarks>
/// <para>
/// <strong><see cref="Inconclusive"/> é o resultado mais comum e o mais importante</strong>
/// (ADR-003): beneficiário ainda não cadastrado, origem nunca vista, pagador não extraível.
/// Colapsá-lo em "reprovado" transformaria operação normal em alarme e treinaria o usuário a
/// ignorar alertas — que é como o alerta que importa passa batido.
/// </para>
/// <para>
/// <strong><see cref="Warning"/> foi acrescentado depois do ADR-003</strong>, pela decisão de
/// 2026-07-31 sobre divergência de nome em arrecadação. Ele existe porque a alternativa não
/// servia: um <c>Failed</c> num check <c>Blocking</c> travaria o pagamento por uma grafia
/// diferente de concessionária, e um <c>Passed</c> jogaria fora a única evidência de
/// beneficiário que arrecadação oferece. <strong>Warning nunca bloqueia</strong>, qualquer que
/// seja a severidade do check — é o que o distingue de <see cref="Failed"/>.
/// </para>
/// </remarks>
public sealed class CheckOutcome : Enumeration
{
    /// <summary>A comparação foi feita e bateu.</summary>
    public static readonly CheckOutcome Passed = new(1, nameof(Passed), isFailure: false, requiresAttention: false);

    /// <summary>A comparação foi feita e não bateu.</summary>
    public static readonly CheckOutcome Failed = new(2, nameof(Failed), isFailure: true, requiresAttention: true);

    /// <summary>Não havia contra o que comparar. Nada foi provado — e nada foi desmentido.</summary>
    public static readonly CheckOutcome Inconclusive = new(3, nameof(Inconclusive), isFailure: false, requiresAttention: true);

    /// <summary>O check não se aplica a este documento. Ausência de dado estrutural, não omissão.</summary>
    public static readonly CheckOutcome Skipped = new(4, nameof(Skipped), isFailure: false, requiresAttention: false);

    /// <summary>Divergência real que merece o olho do aprovador, mas que não sustenta uma reprovação.</summary>
    public static readonly CheckOutcome Warning = new(5, nameof(Warning), isFailure: false, requiresAttention: true);

    /// <summary>Só <see cref="Failed"/> é falha. É o único que pode reprovar um boleto.</summary>
    public bool IsFailure { get; }

    /// <summary>Deve aparecer destacado na tela de aprovação como "requer atenção".</summary>
    public bool RequiresAttention { get; }

    private CheckOutcome(int id, string name, bool isFailure, bool requiresAttention) : base(id, name)
    {
        IsFailure = isFailure;
        RequiresAttention = requiresAttention;
    }
}
