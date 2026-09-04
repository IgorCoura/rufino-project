namespace BillPayment.Domain.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.TrustedOrigins;

/// <summary>
/// O que a busca global por duplicata encontrou.
/// </summary>
/// <remarks>
/// A distinção entre os dois achados não é cosmética: quando a Bill existente é de outro
/// tenant, a evidência é um aviso <strong>genérico</strong> — sem id, sem nome, sem valor —
/// porque dizer de quem é seria vazamento entre contas (ADR-008).
/// </remarks>
public sealed class DuplicateFinding : Enumeration
{
    public static readonly DuplicateFinding None = new(1, nameof(None));
    public static readonly DuplicateFinding SameTenant = new(2, nameof(SameTenant));
    public static readonly DuplicateFinding OtherTenant = new(3, nameof(OtherTenant));

    private DuplicateFinding(int id, string name) : base(id, name) { }

    /// <summary>
    /// Traduz o que a busca global encontrou no achado que a verificação consome.
    /// </summary>
    /// <remarks>
    /// Vive aqui, e não no handler, porque decidir que "existe e é de outra conta" é um achado
    /// diferente de "existe e é minha" é regra de domínio — é dela que sai a diferença entre
    /// apontar o boleto original e devolver o aviso genérico do ADR-008.
    /// </remarks>
    public static DuplicateFinding From(DuplicateProbe probe)
    {
        ArgumentNullException.ThrowIfNull(probe);

        if (!probe.Exists)
            return None;

        return probe.BelongsToTenant ? SameTenant : OtherTenant;
    }
}

/// <summary>
/// Tudo que o serviço de validação precisa para apurar as doze verificações, já resolvido.
/// </summary>
/// <remarks>
/// <para>
/// É um objeto de parâmetro, não um Value Object de domínio: carrega agregados e uma porta.
/// Existe porque o serviço é <strong>puro e síncrono</strong> — todo I/O (consulta ao provedor,
/// busca de duplicata, carga dos cadastros) acontece antes, no handler. Assim a apuração inteira
/// é testável sem banco, sem rede e sem relógio.
/// </para>
/// <para>
/// <see cref="BankDirectory"/> é a única porta aqui, e entra por ser um snapshot em memória —
/// não faz I/O, não tem <c>CancellationToken</c> e não pode ficar indisponível.
/// </para>
/// </remarks>
public sealed class BillValidationContext
{
    public required Bill Bill { get; init; }

    /// <summary>Resultado da consulta do código de barras nesta rodada. Nulo quando não há barcode.</summary>
    public BillLookupResult? BankSlipLookup { get; init; }

    /// <summary>Resultado do decode do QR nesta rodada. Nulo quando não há QR Pix.</summary>
    public PixLookupResult? PixLookup { get; init; }

    public required PayeeResolution PayeeResolution { get; init; }

    /// <summary>Origem cadastrada que casou com o remetente. Nula quando nunca foi vista.</summary>
    public TrustedOrigin? Origin { get; init; }

    /// <summary>Cadastro fiscal do tenant. Nulo quando o onboarding não foi concluído.</summary>
    public PayerProfile? PayerProfile { get; init; }

    public required IBankDirectory BankDirectory { get; init; }

    public DuplicateFinding Duplicate { get; init; } = DuplicateFinding.None;

    /// <summary>Id da Bill original quando a duplicata é do mesmo tenant. Nunca preenchido para outro tenant.</summary>
    public BillId? DuplicateOf { get; init; }

    public required DateOnly Today { get; init; }

    /// <summary>Hora corrente, para o corte de agendamento do provedor.</summary>
    public required TimeOnly TimeOfDay { get; init; }
}
